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
        public event Action<string>? FolderChanged; // Path of folder that changed, not currently used

        // Database variables
        private readonly string assetsPath;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Opens a new asset database instance at an asset path.
        /// </summary>
        /// <param name="assetsPath">Asset path to open</param>
        public AssetDatabase(string assetsPath)
        {
            this.assetsPath = assetsPath;
            jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
            Directory.CreateDirectory(assetsPath); // Ensure Assets folder exists
            InitializeDatabase();
        }

        /// <summary>
        /// Scans all folders and loads/creates metadata.
        /// </summary>
        private void InitializeDatabase()
        {
            AllAssetsByID.Clear();
            Folders.Clear();

            // Get all directories including root
            List<string> directories = [assetsPath, .. Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories)];
            foreach (string dir in directories)
                ProcessFolder(dir);
            Debug.Info($"Asset Database: Loaded {AllAssetsByID.Count} assets from {Folders.Count} folders");
        }

        private void ProcessFolder(string folderPath)
        {
            string relativeFolder = Path.GetRelativePath(assetsPath, folderPath);
            Debug.Warning("Relative folder: " + relativeFolder);
            string folderName = Path.GetFileName(folderPath) == "" ? "Root" : Path.GetFileName(folderPath);
            Debug.Warning("Asset Folder Name: " + folderName);
            string expectedMetaFileName = folderName + ".divmeta";
            Debug.Warning("Expected Meta Name: " + expectedMetaFileName);
            string metadataPath = Path.Combine(folderPath, expectedMetaFileName);
            Debug.Warning("Meta Path: " + metadataPath);

            // Get all asset files (excluding any .divmeta files)
            Dictionary<string, string> assetFiles = Directory.GetFiles(folderPath)
                .Where(f => !f.EndsWith(".divmeta", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase);

            // Get all .divmeta files in the folder
            string[] allMetaFiles = Directory.GetFiles(folderPath, "*.divmeta", SearchOption.TopDirectoryOnly);

            FolderMetadata folderMeta;
            if (File.Exists(metadataPath))
            {
                // Load existing metadata
                string json = File.ReadAllText(metadataPath);
                folderMeta = JsonSerializer.Deserialize<FolderMetadata>(json, jsonSerializerOptions)
                    ?? new FolderMetadata { FolderPath = relativeFolder };

                // Validate that the loaded folder path matches
                if (folderMeta.FolderPath != relativeFolder)
                {
                    Debug.Warning($"Folder path mismatch in {metadataPath}: expected '{relativeFolder}', found '{folderMeta.FolderPath}'. Updating.");
                    folderMeta.FolderPath = relativeFolder;
                }

                // Delete any other stray .divmeta files (from previous folder names)
                foreach (string oldMetaFile in allMetaFiles)
                {
                    if (oldMetaFile != metadataPath)
                    {
                        try
                        {
                            File.Delete(oldMetaFile);
                            Debug.Info($"Deleted old metadata file: {oldMetaFile}");
                        }
                        catch (Exception ex)
                        {
                            Debug.Error($"Failed to delete old metadata file {oldMetaFile}", ex);
                        }
                    }
                }

                // Check for deleted files
                foreach (string filename in folderMeta.Assets.Keys.ToList())
                {
                    if (!assetFiles.ContainsKey(filename))
                    {
                        // File was deleted
                        AssetMetadata asset = folderMeta.Assets[filename];
                        folderMeta.Assets.Remove(filename);
                        AllAssetsByID.Remove(asset.ID);
                        Debug.Info($"Asset removed: {asset.FileName}");
                    }
                }

                // Check for new/modified files
                foreach (var kvp in assetFiles)
                {
                    string filename = kvp.Key;
                    string fullPath = kvp.Value;
                    DateTime lastModified = File.GetLastWriteTime(fullPath);

                    if (folderMeta.Assets.TryGetValue(filename, out AssetMetadata? existingAsset))
                    {
                        // Update if modified
                        if (lastModified > existingAsset.LastModified)
                        {
                            UpdateAssetMetadata(existingAsset, fullPath);
                            Debug.Info($"Asset updated: {existingAsset.FileName}");
                        }
                    }
                    else
                    {
                        // New asset
                        AssetMetadata newAsset = CreateAssetMetadata(fullPath);
                        folderMeta.Assets[filename] = newAsset;
                        AllAssetsByID[newAsset.ID] = newAsset;
                        Debug.Info($"Asset added: {newAsset.FileName} (GUID: {newAsset.ID})");
                    }
                }
            }
            else
            {
                // No metadata file – create new
                folderMeta = new FolderMetadata { FolderPath = relativeFolder };

                foreach (string file in assetFiles.Values)
                {
                    AssetMetadata asset = CreateAssetMetadata(file);
                    folderMeta.Assets[Path.GetFileName(file)] = asset;
                    AllAssetsByID[asset.ID] = asset;
                    Debug.Info($"Asset discovered: {asset.FileName} (GUID: {asset.ID})");
                }

                // Immediately save the new metadata file so it exists on disk
                SaveFolderMetadata(folderPath, folderMeta);
            }

            Folders[relativeFolder] = folderMeta;
        }

        /// <summary>
        /// Saves all folder metadata files (call on project exit).
        /// </summary>
        public void SaveAll()
        {
            foreach (var kvp in Folders)
            {
                string folderPath = Path.Combine(assetsPath, kvp.Key);
                SaveFolderMetadata(folderPath, kvp.Value);
            }
            Debug.Info("Asset Database: All metadata saved.");
        }

        private void SaveFolderMetadata(string folderPath, FolderMetadata metadata)
        {
            string folderName = Path.GetFileName(folderPath) == "" ? "Root" : Path.GetFileName(folderPath);
            string metadataPath = Path.Combine(folderPath, folderName + ".divmeta");
            string json = JsonSerializer.Serialize(metadata, jsonSerializerOptions);
            File.WriteAllText(metadataPath, json);
        }

        private AssetMetadata CreateAssetMetadata(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return new AssetMetadata
            {
                // ID is auto-generated by constructor, but we can set it explicitly if needed.
                // Here we rely on the default constructor generating a new GUID.
                FileName = Path.GetFileName(filePath),
                RelativePath = Path.GetRelativePath(assetsPath, filePath),
                Type = DetermineAssetType(filePath),
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
            };
        }

        private static void UpdateAssetMetadata(AssetMetadata metadata, string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            metadata.LastModified = fileInfo.LastWriteTime;
            metadata.FileSize = fileInfo.Length;
            // GUID remains unchanged
        }

        private static AssetType DetermineAssetType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch 
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" => AssetType.Texture,
                ".obj" or ".fbx" or ".gltf" => AssetType.SDF,
                ".mat" => AssetType.Material,
                ".shader" or ".hlsl" or ".cs" => AssetType.Script,
                ".sdf" or ".sdfLib" => AssetType.SDF,
                ".wav" or ".mp3" or ".ogg" => AssetType.Audio,
                ".ttf" or ".otf" => AssetType.Font,
                _ => AssetType.None,
            };
        }

        // ----------
        // Public API
        // ----------

        /// <summary>
        /// Gets an asset metadata by its GUID.
        /// </summary>
        public AssetMetadata? GetAssetMetadataByID(string? id) =>
            AllAssetsByID.TryGetValue(id ?? string.Empty, out var asset) ? asset : null;

        /// <summary>
        /// Gets all assets in a folder path relative to the asset folder.
        /// </summary>
        public IEnumerable<AssetMetadata> GetAssetsInFolder(string relativeFolder)
        {
            if (Folders.TryGetValue(relativeFolder, out var folder))
            {
                Debug.Info($"GetAssetsInFolder('{relativeFolder}') found {folder.Assets.Count} assets");
                return folder.Assets.Values;
            }

            Debug.Warning($"GetAssetsInFolder('{relativeFolder}') - folder not found");
            return [];
        }

        /// <summary>
        /// Gets all the assets in the project database.
        /// </summary>
        public IEnumerable<AssetMetadata> GetAllAssets() => AllAssetsByID.Values;

        /// <summary>
        /// Gets an enumerable of all asset metadatas of a type.
        /// </summary>
        public IEnumerable<AssetMetadata> GetAssetsByType(AssetType type) =>
            AllAssetsByID.Values.Where(a => a.Type == type);

        /// <summary>
        /// Gets the full filesystem path for an asset.
        /// </summary>
        public string? GetAssetFullPath(string id)
        {
            AssetMetadata? asset = GetAssetMetadataByID(id);
            return asset != null ? Path.Combine(assetsPath, asset.RelativePath) : null;
        }

        public void Dispose()
        {
            //GC.SuppressFinalize(this);
            // Implemnent later, nothing to dispose
        }

        // Optional: Import, Delete, Rename (can be added later)

    }
}
