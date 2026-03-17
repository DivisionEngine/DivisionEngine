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
using System.Reflection;
using System.Text.Json;

namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Manages the asset database for the currently loaded project.
    /// </summary>
    public static class AssetDatabase
    {
        // Database variables
        private static readonly JsonSerializerOptions jsonSerializerOptions =
            new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Contains all the folder metadatas.
        /// </summary>
        public  static Dictionary<string, FolderMetadata> Folders { get; private set; } = []; // Key = relative folder path

        /// <summary>
        /// Contains all the asset metadatas.
        /// </summary>
        public static Dictionary<string, AssetMetadata> AllAssetsByID { get; private set; } = []; // Master GUID

        /// <summary>
        /// Called when an assets folder is changed.
        /// </summary>
        /// <remarks>Path of folder that changed, not currently used</remarks>
        public static event Action<string>? FolderChanged;

        /// <summary>
        /// Current project path, if loaded.
        /// </summary>
        public static string ProjectPath => ProjectManager.CurrentProjectPath
            ?? throw new InvalidOperationException("Asset Database: No project loaded");

        /// <summary>
        /// Current assets path for project.
        /// </summary>
        public static string AssetsPath => Path.Combine(ProjectPath, "Assets");

        /// <summary>
        /// Gets a path relative to the project folder.
        /// </summary>
        /// <param name="folderPath">Folder path</param>
        /// <returns>Sub path of folder</returns>
        public static string GetProjectRelativePath(string folderPath) =>
            Path.GetRelativePath(ProjectPath, folderPath);

        /// <summary>
        /// Scans all folders and loads/creates metadata.
        /// </summary>
        public static void Initialize()
        {
            AllAssetsByID.Clear();
            Folders.Clear();

            // Ensures assets path exists
            Directory.CreateDirectory(AssetsPath);

            // Get all directories including root
            List<string> directories = [AssetsPath, .. Directory.GetDirectories(AssetsPath, "*", SearchOption.AllDirectories)];
            foreach (string dir in directories) ProcessFolder(dir);

            Debug.Info($"Asset Database: Loaded {AllAssetsByID.Count} assets from {Folders.Count} folders");
        }

        private static void ProcessFolder(string folderPath)
        {
            string relativeFolder = GetProjectRelativePath(folderPath);
            string folderName = Path.GetFileName(folderPath);
            string expectedMetaFileName = folderName + ".divmeta";
            string metadataPath = Path.Combine(folderPath, expectedMetaFileName);

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

                // Add all existing assets to AllAssetsByID
                foreach (AssetMetadata asset in folderMeta.Assets.Values)
                    if (!AllAssetsByID.ContainsKey(asset.ID))
                        AllAssetsByID[asset.ID] = asset;

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

                // Immediately save the new metadata file
                SaveFolderMetadata(folderPath, folderMeta);
            }

            Folders[relativeFolder] = folderMeta;
        }

        /// <summary>
        /// Saves all folder metadata files (call on project exit).
        /// </summary>
        public static void SaveAll()
        {
            foreach (var kvp in Folders)
            {
                string folderPath = Path.Combine(ProjectPath, kvp.Key);
                SaveFolderMetadata(folderPath, kvp.Value);
            }
            Debug.Info("Asset Database: All metadata saved");
        }

        private static void SaveFolderMetadata(string folderPath, FolderMetadata metadata)
        {
            string folderName = Path.GetFileName(folderPath);
            string metadataPath = Path.Combine(folderPath, folderName + ".divmeta");
            string json = JsonSerializer.Serialize(metadata, jsonSerializerOptions);
            File.WriteAllText(metadataPath, json);
        }

        private static AssetMetadata CreateAssetMetadata(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return new AssetMetadata
            {
                // ID is auto-generated by constructor, but we can set it explicitly if needed.
                // Here we rely on the default constructor generating a new GUID.
                FileName = Path.GetFileName(filePath),
                RelativePath = GetProjectRelativePath(filePath),
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
        /// Gets an asset type from <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of asset</typeparam>
        /// <returns>AssetType identifier</returns>
        public static AssetType GetAssetType<T>() where T : Asset => GetAssetType(typeof(T));

        /// <summary>
        /// Gets an asset type from Type.
        /// </summary>
        /// <param name="type">Type of asset</param>
        /// <returns>AssetType identifier</returns>
        public static AssetType GetAssetType(Type type) => 
            type.GetCustomAttribute<AssetTypeAttribute>()?.Type ?? AssetType.None;

        /// <summary>
        /// Gets an asset metadata by its GUID.
        /// </summary>
        public static AssetMetadata? GetAssetMetadataByID(string? id) =>
            AllAssetsByID.TryGetValue(id ?? string.Empty, out var asset) ? asset : null;

        /// <summary>
        /// Gets all assets in a folder path relative to the asset folder.
        /// </summary>
        public static IEnumerable<AssetMetadata> GetAssetsInFolder(string relativeFolder)
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
        public static IEnumerable<AssetMetadata> GetAllAssets() => AllAssetsByID.Values;

        /// <summary>
        /// Gets an enumerable of all asset metadatas of a type.
        /// </summary>
        public static IEnumerable<AssetMetadata> GetAssetsByType(AssetType type) =>
            AllAssetsByID.Values.Where(a => a.Type == type);

        /// <summary>
        /// Gets the full filesystem path for an asset.
        /// </summary>
        public static string? GetAssetFullPath(string id)
        {
            AssetMetadata? asset = GetAssetMetadataByID(id);
            return asset != null ? Path.Combine(ProjectPath, asset.RelativePath) : null;
        }

        // Optional: Import, Delete, Rename (can be added later)

    }
}
