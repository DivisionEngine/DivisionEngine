//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using System.Text.Json;

namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Manages the asset database for the currently loaded project.
    /// </summary>
    public class AssetDatabase
    {
        // Asset database events
        public event Action<string>? FolderChanged; // Path of folder that changed

        // Private database variables
        private readonly string assetsPath, dbPath;
        private ProjectAssetDatabase projectDB;
        private readonly FileSystemWatcher fileWatcher;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        private readonly HashSet<string> pendingNotifications = new();
        private readonly System.Timers.Timer notificationThrottle;

        /// <summary>
        /// Opens a new asset database instance at an asset path.
        /// </summary>
        /// <param name="assetsPath">Asset path to open</param>
        public AssetDatabase(string assetsPath)
        {
            this.assetsPath = assetsPath;
            projectDB = new ProjectAssetDatabase { ProjectPath = assetsPath };
            dbPath = Path.Combine(assetsPath, "..", "ProjectAssets.divdb");

            jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
            Directory.CreateDirectory(assetsPath); // Ensure Assets folder exists
            LoadProjectDatabase(); // Load existing database or create new

            // Throttle notifications to avoid too many refreshes
            notificationThrottle = new System.Timers.Timer(100);
            notificationThrottle.AutoReset = false;
            notificationThrottle.Elapsed += (s, e) =>
            {
                lock (pendingNotifications)
                {
                    foreach (string? folder in pendingNotifications)
                        FolderChanged?.Invoke(folder);
                    pendingNotifications.Clear();
                }
            };

            // Setup file watcher for realtime updates
            fileWatcher = new FileSystemWatcher(assetsPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            fileWatcher.Created += OnFileChanged;
            fileWatcher.Changed += OnFileChanged;
            fileWatcher.Deleted += OnFileDeleted;
            fileWatcher.Renamed += OnFileRenamed;
        }

        public void ScanAllFolders()
        {
            foreach (string? folder in Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories))
                ScanFolder(folder);
            ScanFolder(assetsPath); // Also scan root
            SaveProjectDatabase();
        }

        private void ScanFolder(string folderPath)
        {
            string relativeFolder = Path.GetRelativePath(assetsPath, folderPath);
            string metadataPath = Path.Combine(folderPath, ".divmeta");

            FolderMetadata folderMeta;
            if (File.Exists(metadataPath))
            {
                // Load existing metadata
                string json = File.ReadAllText(metadataPath);
                folderMeta = JsonSerializer.Deserialize<FolderMetadata>(json) ?? new FolderMetadata { FolderPath = relativeFolder };

                // Check for deleted files
                var currentFiles = Directory.GetFiles(folderPath)
                    .Where(f => !Path.GetFileName(f).StartsWith('.'))
                    .ToDictionary(f => Path.GetFileName(f));

                foreach (string? filename in folderMeta.Assets.Keys.ToList())
                {
                    if (!currentFiles.ContainsKey(filename))
                    {
                        // File was deleted
                        AssetMetadata? asset = folderMeta.Assets[filename];
                        projectDB.AllAssetsByID.Remove(asset.ID);
                        folderMeta.Assets.Remove(filename);
                    }
                }

                // Check for new/modified files
                foreach (var file in currentFiles)
                {
                    string fullPath = file.Value;
                    DateTime lastModified = File.GetLastWriteTime(fullPath);

                    if (folderMeta.Assets.TryGetValue(file.Key, out AssetMetadata? existingAsset))
                    {
                        // Update metadata if modified
                        if (lastModified > existingAsset.LastModified)
                            UpdateAssetMetadata(ref existingAsset, fullPath);
                    }
                    else
                    {
                        // New asset
                        AssetMetadata? newAsset = CreateAssetMetadata(fullPath, relativeFolder);
                        folderMeta.Assets[file.Key] = newAsset;
                        projectDB.AllAssetsByID[newAsset.ID] = newAsset;
                    }
                }
            }
            else
            {
                // Create new folder metadata
                folderMeta = new FolderMetadata { FolderPath = relativeFolder };
                foreach (var file in Directory.GetFiles(folderPath).Where(f => !Path.GetFileName(f).StartsWith('.')))
                {
                    AssetMetadata? asset = CreateAssetMetadata(file, relativeFolder);
                    folderMeta.Assets[Path.GetFileName(file)] = asset;
                    projectDB.AllAssetsByID[asset.ID] = asset;
                }
            }

            folderMeta.LastScanTime = DateTime.Now;
            projectDB.Folders[relativeFolder] = folderMeta;

            // Save folder metadata
            string folderJson = JsonSerializer.Serialize(folderMeta, jsonSerializerOptions);
            File.WriteAllText(metadataPath, folderJson);
        }

        private static AssetMetadata CreateAssetMetadata(string filePath, string relativeFolder)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return new AssetMetadata
            {
                FileName = Path.GetFileName(filePath),
                RelativePath = string.IsNullOrEmpty(relativeFolder)
                    ? Path.GetFileName(filePath)
                    : Path.Combine(relativeFolder, Path.GetFileName(filePath)).Replace('\\', '/'),
                Type = DetermineAssetType(filePath),
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length
            };
        }

        /// <summary>
        /// Updates an asset metadata file.
        /// </summary>
        /// <param name="metadata">Asset metadata to update</param>
        /// <param name="filePath">File path to pull updates from</param>
        private static void UpdateAssetMetadata(ref AssetMetadata metadata, string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            metadata.LastModified = fileInfo.LastWriteTime;
            metadata.FileSize = fileInfo.Length;
            // Don't change GUID or other permanent properties
        }

        /// <summary>
        /// Finds the correct asset type based on its file path.
        /// </summary>
        /// <param name="filePath">Path of asset file</param>
        /// <returns>Asset file type</returns>
        private static AssetType DetermineAssetType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" => AssetType.Texture,
                ".obj" or ".fbx" or ".gltf" => AssetType.SDF,
                ".mat" => AssetType.Material,
                ".shader" or ".hlsl" => AssetType.Script,
                ".cs" when filePath.Contains("ComputeShaders") => AssetType.Script,
                ".sdf" or ".sdfLib" => AssetType.SDF,
                ".wav" or ".mp3" or ".ogg" => AssetType.Audio,
                ".ttf" or ".otf" => AssetType.Font,
                _ => AssetType.None
            };
        }

        /// <summary>
        /// Loads the project asset database.
        /// </summary>
        private void LoadProjectDatabase()
        {
            if (File.Exists(dbPath))
            {
                string json = File.ReadAllText(dbPath);
                projectDB = JsonSerializer.Deserialize<ProjectAssetDatabase>(json, jsonSerializerOptions)
                    ?? new ProjectAssetDatabase { ProjectPath = assetsPath }; // Fallback if project asset database could not be loaded
            }
        }

        /// <summary>
        /// Saves the project database file.
        /// </summary>
        private void SaveProjectDatabase()
        {
            string json = JsonSerializer.Serialize(projectDB, jsonSerializerOptions);
            File.WriteAllText(dbPath, json);
        }

        // File watcher event handlers

        /// <summary>
        /// Called when a file is added or modified in the asset folder.
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Queue a rescan of the affected folder
            string folder = Path.GetDirectoryName(e.FullPath)!;
            ScanFolder(folder);

            // Queue notification
            lock (pendingNotifications)
            {
                pendingNotifications.Add(folder);
                notificationThrottle.Stop();
                notificationThrottle.Start();
            }
        }

        /// <summary>
        /// Called when a file is deleted in the asset folder.
        /// </summary>
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            string folder = Path.GetDirectoryName(e.FullPath)!;
            ScanFolder(folder);
            lock (pendingNotifications)
            {
                pendingNotifications.Add(folder);
                notificationThrottle.Stop();
                notificationThrottle.Start();
            }
        }

        /// <summary>
        /// Called when a file is renamed in the asset folder.
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            string oldFolder = Path.GetDirectoryName(e.OldFullPath)!;
            string newFolder = Path.GetDirectoryName(e.FullPath)!;

            // Scan both old and new folders
            ScanFolder(oldFolder);
            ScanFolder(newFolder);

            lock (pendingNotifications)
            {
                pendingNotifications.Add(oldFolder);
                pendingNotifications.Add(newFolder);
                notificationThrottle.Stop();
                notificationThrottle.Start();
            }
        }

        // Query methods

        /// <summary>
        /// Gets an asset metadata by its GUID.
        /// </summary>
        /// <param name="id">Asset GUID</param>
        /// <returns>Asset metadata with GUID</returns>
        public AssetMetadata? GetAssetByID(string id) =>
            projectDB.AllAssetsByID.TryGetValue(id, out var asset) ? asset : null;

        /// <summary>
        /// Gets all assets in a folder path relative to the asset folder.
        /// </summary>
        /// <param name="relativeFolder">Relative asset folder path</param>
        /// <returns>Enumerable of asset metadatas in relative folder</returns>
        public IEnumerable<AssetMetadata> GetAssetsInFolder(string relativeFolder) => 
            projectDB.Folders.TryGetValue(relativeFolder, out var folder) ? folder.Assets.Values : [];

        /// <summary>
        /// Gets all the assets in the project database.
        /// </summary>
        /// <returns>Enumerable of all asset metadatas</returns>
        public IEnumerable<AssetMetadata> GetAllAssets() => projectDB.AllAssetsByID.Values;

        /// <summary>
        /// Gets an enumerable of all asset metadatas of a type.
        /// </summary>
        /// <param name="type">Asset type to test for</param>
        /// <returns>Asset metadatas organized by asset type</returns>
        public IEnumerable<AssetMetadata> GetAssetsByType(AssetType type) =>
            projectDB.AllAssetsByID.Values.Where(a => a.Type == type);
    }
}
