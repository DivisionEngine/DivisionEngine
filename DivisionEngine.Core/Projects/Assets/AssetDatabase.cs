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
    public class AssetDatabase : IDisposable
    {
        /// <summary>
        /// Contains all the folder metadatas.
        /// </summary>
        public Dictionary<string, FolderMetadata> Folders { get; private set; } = []; // Key = relative folder path

        /// <summary>
        /// Contains all the asset metadatas.
        /// </summary>
        public Dictionary<string, AssetMetadata> AllAssetsByID { get; private set; } = []; // Master GUID

        // Events for UI updates
        public event Action<string>? FolderChanged; // Path of folder that changed
        public event Action<AssetMetadata>? AssetAdded;
        public event Action<AssetMetadata>? AssetRemoved;
        public event Action<AssetMetadata>? AssetUpdated;

        // Database variables
        private readonly string assetsPath;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        private readonly FileSystemWatcher fileWatcher;
        private readonly HashSet<string> pendingNotifications = [];
        private readonly System.Timers.Timer notificationThrottle;
        private bool isDisposed = false;


        /// <summary>
        /// Opens a new asset database instance at an asset path.
        /// </summary>
        /// <param name="assetsPath">Asset path to open</param>
        public AssetDatabase(string assetsPath)
        {
            this.assetsPath = assetsPath;
            jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
            Directory.CreateDirectory(assetsPath); // Ensure Assets folder exists

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
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                               NotifyFilters.LastWrite | NotifyFilters.Size,
            };

            fileWatcher.Created += OnFileChanged;
            fileWatcher.Changed += OnFileChanged;
            fileWatcher.Deleted += OnFileDeleted;
            fileWatcher.Renamed += OnFileRenamed;

            ScanAllFolders();
        }

        /// <summary>
        /// Scans and updates all folders in the database.
        /// </summary>
        public void ScanAllFolders()
        {
            // Clear existing data
            Folders.Clear();
            AllAssetsByID.Clear();

            foreach (string? folder in Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories))
                ScanFolder(folder);
            ScanFolder(assetsPath); // Also scan root
            Debug.Info($"Asset Database: Loaded {AllAssetsByID.Count} assets from {Folders.Count} folders");
        }

        /// <summary>
        /// Scans a single folder and updates its metadata.
        /// </summary>
        /// <param name="folderPath">Full path to folder</param>
        /// <param name="saveMetadata">Whether to save the metadata file</param>
        /// <returns>The folder metadata</returns>
        private FolderMetadata ScanFolder(string folderPath, bool saveMetadata = true)
        {
            string relativeFolder = Path.GetRelativePath(assetsPath, folderPath);
            string metadataPath = Path.Combine(folderPath, $"{Path.GetFileName(folderPath)}.divmeta");

            FolderMetadata folderMeta;
            Dictionary<string, AssetMetadata> oldAssets = [];

            // Check if we already have this folder loaded
            if (Folders.TryGetValue(relativeFolder, out var existingFolder))
            {
                // Store old assets for change detection
                foreach (var kvp in existingFolder.Assets)
                    oldAssets[kvp.Key] = kvp.Value;
            }

            if (File.Exists(metadataPath))
            {
                // Load existing metadata
                string json = File.ReadAllText(metadataPath);
                folderMeta = JsonSerializer.Deserialize<FolderMetadata>(json, jsonSerializerOptions)
                    ?? new FolderMetadata { FolderPath = relativeFolder };

                // Get current files (excluding metadata files)
                var currentFiles = Directory.GetFiles(folderPath)
                    .Where(f => !Path.GetFileName(f).EndsWith(".divmeta", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

                // Check for deleted files
                foreach (string filename in folderMeta.Assets.Keys.ToList())
                {
                    if (!currentFiles.ContainsKey(filename))
                    {
                        // File was deleted
                        AssetMetadata asset = folderMeta.Assets[filename];
                        folderMeta.Assets.Remove(filename);
                        AllAssetsByID.Remove(asset.ID);
                        AssetRemoved?.Invoke(asset);
                        Debug.Info($"Asset removed: {asset.FileName}");
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
                        {
                            UpdateAssetMetadata(existingAsset, fullPath);
                            AssetUpdated?.Invoke(existingAsset);
                            Debug.Info($"Asset updated: {existingAsset.FileName}");
                        }
                    }
                    else
                    {
                        // New asset - preserve old GUID if file was just renamed
                        AssetMetadata? oldAsset = null;
                        if (oldAssets.TryGetValue(file.Key, out var possibleOld))
                        {
                            // Same filename, different content? Probably not a rename
                        }

                        AssetMetadata newAsset = CreateAssetMetadata(fullPath, relativeFolder);
                        folderMeta.Assets[file.Key] = newAsset;
                        AllAssetsByID[newAsset.ID] = newAsset;
                        AssetAdded?.Invoke(newAsset);
                        Debug.Info($"Asset added: {newAsset.FileName} (GUID: {newAsset.ID})");
                    }
                }
            }
            else
            {
                // Create new folder metadata
                folderMeta = new FolderMetadata { FolderPath = relativeFolder };

                foreach (string file in Directory.GetFiles(folderPath)
                    .Where(f => !Path.GetFileName(f).EndsWith(".divmeta", StringComparison.OrdinalIgnoreCase)))
                {
                    AssetMetadata asset = CreateAssetMetadata(file, relativeFolder);
                    folderMeta.Assets[Path.GetFileName(file)] = asset;
                    AllAssetsByID[asset.ID] = asset;
                    AssetAdded?.Invoke(asset);
                    Debug.Info($"Asset discovered: {asset.FileName} (GUID: {asset.ID})");
                }
            }

            folderMeta.LastScanTime = DateTime.Now;
            Folders[relativeFolder] = folderMeta;

            // Save folder metadata if requested
            if (saveMetadata)
            {
                string folderJson = JsonSerializer.Serialize(folderMeta, jsonSerializerOptions);
                File.WriteAllText(metadataPath, folderJson);
            }

            return folderMeta;
        }


        /// <summary>
        /// Creates the asset metadata.
        /// </summary>
        /// <param name="filePath">File path of asset</param>
        /// <param name="relativeFolder">Folder relative to file path</param>
        /// <returns>Generated asset metadata</returns>
        private AssetMetadata CreateAssetMetadata(string filePath, string relativeFolder)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return new AssetMetadata
            {
                FileName = Path.GetFileName(filePath),
                RelativePath = Path.GetRelativePath(assetsPath, filePath),
                Type = DetermineAssetType(filePath),
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
            };
        }

        /// <summary>
        /// Updates existing asset metadata without changing GUID.
        /// </summary>
        private void UpdateAssetMetadata(AssetMetadata metadata, string filePath)
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
                _ => AssetType.None,
            };
        }

        // File watcher event handlers

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (IsMetadataFile(e.Name)) return;
            if (IsDisposed) return;

            string folder = Path.GetDirectoryName(e.FullPath)!;
            var folderMeta = ScanFolder(folder);

            // Queue notification
            lock (pendingNotifications)
            {
                pendingNotifications.Add(folder);
                notificationThrottle.Stop();
                notificationThrottle.Start();
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (IsMetadataFile(e.Name)) return;
            if (IsDisposed) return;

            string folder = Path.GetDirectoryName(e.FullPath)!;
            var folderMeta = ScanFolder(folder);

            lock (pendingNotifications)
            {
                pendingNotifications.Add(folder);
                notificationThrottle.Stop();
                notificationThrottle.Start();
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (IsMetadataFile(e.Name) || IsMetadataFile(e.OldName)) return;
            if (IsDisposed) return;

            string oldFolder = Path.GetDirectoryName(e.OldFullPath)!;
            string newFolder = Path.GetDirectoryName(e.FullPath)!;

            // Scan both old and new folders
            if (oldFolder == newFolder)
            {
                // Same folder - just rename
                ScanFolder(oldFolder);
            }
            else
            {
                // Moved between folders - scan both
                ScanFolder(oldFolder);
                ScanFolder(newFolder);
            }

            lock (pendingNotifications)
            {
                pendingNotifications.Add(oldFolder);
                pendingNotifications.Add(newFolder);
                notificationThrottle.Stop();
                notificationThrottle.Start();
            }
        }

        private bool IsMetadataFile(string? fileName)
        {
            return fileName?.EndsWith(".divmeta", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private bool IsDisposed => isDisposed;

        // Public API

        /// <summary>
        /// Refreshes a specific folder.
        /// </summary>
        public void RefreshFolder(string folderPath)
        {
            if (!folderPath.StartsWith(assetsPath))
                folderPath = Path.Combine(assetsPath, folderPath);

            if (Directory.Exists(folderPath))
            {
                ScanFolder(folderPath);
                FolderChanged?.Invoke(folderPath);
            }
        }

        /// <summary>
        /// Gets an asset metadata by its GUID.
        /// </summary>
        /// <param name="id">Asset GUID</param>
        /// <returns>Asset metadata with GUID</returns>
        public AssetMetadata? GetAssetMetadataByID(string id) =>
            AllAssetsByID.TryGetValue(id, out var asset) ? asset : null;

        /// <summary>
        /// Gets all assets in a folder path relative to the asset folder.
        /// </summary>
        /// <param name="relativeFolder">Relative asset folder path</param>
        /// <returns>Enumerable of asset metadatas in relative folder</returns>
        public IEnumerable<AssetMetadata> GetAssetsInFolder(string relativeFolder) => 
            Folders.TryGetValue(relativeFolder, out var folder) ? folder.Assets.Values : [];

        /// <summary>
        /// Gets all the assets in the project database.
        /// </summary>
        /// <returns>Enumerable of all asset metadatas</returns>
        public IEnumerable<AssetMetadata> GetAllAssets() => AllAssetsByID.Values;

        /// <summary>
        /// Gets an enumerable of all asset metadatas of a type.
        /// </summary>
        /// <param name="type">Asset type to test for</param>
        /// <returns>Asset metadatas organized by asset type</returns>
        public IEnumerable<AssetMetadata> GetAssetsByType(AssetType type) =>
            AllAssetsByID.Values.Where(a => a.Type == type);

        /// <summary>
        /// Gets the full filesystem path for an asset.
        /// </summary>
        public string? GetAssetFullPath(string id)
        {
            var asset = GetAssetMetadataByID(id);
            return asset != null ? Path.Combine(assetsPath, asset.RelativePath) : null;
        }

        /// <summary>
        /// Imports an existing file into the asset database.
        /// </summary>
        public AssetMetadata? ImportAsset(string sourceFilePath, string destinationFolder)
        {
            if (!File.Exists(sourceFilePath))
                return null;

            string fileName = Path.GetFileName(sourceFilePath);
            string destPath = Path.Combine(assetsPath, destinationFolder, fileName);

            // Ensure destination directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

            // Copy file
            File.Copy(sourceFilePath, destPath, overwrite: true);

            // Scan the destination folder
            string folderPath = Path.GetDirectoryName(destPath)!;
            var folderMeta = ScanFolder(folderPath);

            // Return the new asset
            return folderMeta.Assets.TryGetValue(fileName, out var asset) ? asset : null;
        }

        /// <summary>
        /// Deletes an asset by GUID.
        /// </summary>
        public bool DeleteAsset(string id)
        {
            var asset = GetAssetMetadataByID(id);
            if (asset == null) return false;

            string fullPath = Path.Combine(assetsPath, asset.RelativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Renames an asset.
        /// </summary>
        public AssetMetadata? RenameAsset(string id, string newName)
        {
            var asset = GetAssetMetadataByID(id);
            if (asset == null) return null;

            string oldPath = Path.Combine(assetsPath, asset.RelativePath);
            string directory = Path.GetDirectoryName(oldPath)!;
            string extension = Path.GetExtension(oldPath);
            string newFileName = newName + extension;
            string newPath = Path.Combine(directory, newFileName);

            if (File.Exists(newPath)) return null; // Name conflict

            File.Move(oldPath, newPath);

            // Scan the folder to update metadata
            string folder = Path.GetDirectoryName(newPath)!;
            var folderMeta = ScanFolder(folder);

            return folderMeta.Assets.TryGetValue(newFileName, out var newAsset) ? newAsset : null;
        }

        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            fileWatcher?.Dispose();
            notificationThrottle?.Dispose();

            FolderChanged = null;
            AssetAdded = null;
            AssetRemoved = null;
            AssetUpdated = null;
        }
    }
}
