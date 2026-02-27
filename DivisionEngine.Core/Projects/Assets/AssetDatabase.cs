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
        /// <summary>
        /// Contains all the folder metadatas.
        /// </summary>
        public Dictionary<string, FolderMetadata> Folders { get; private set; } = []; // Key = relative folder path

        /// <summary>
        /// Contains all the asset metadatas.
        /// </summary>
        public Dictionary<string, AssetMetadata> AllAssetsByID { get; private set; } = []; // Master GUID

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
            ScanAllFolders();
            InitAllAssets();
        }

        /// <summary>
        /// Scans and updates all folders in the database.
        /// </summary>
        public void ScanAllFolders()
        {
            foreach (string? folder in Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories))
                ScanFolder(folder);
            ScanFolder(assetsPath); // Also scan root
        }

        /// <summary>
        /// Initializes the asset GUID database.
        /// </summary>
        public void InitAllAssets()
        {
            AllAssetsByID.Clear();
            foreach (var folder in Folders.Values)
            {
                foreach (AssetMetadata asset in folder.Assets.Values)
                    AllAssetsByID.Add(asset.ID, asset);
            }
            Debug.Info($"Asset Database: Loaded {AllAssetsByID.Count} assets");
        }

        private void ScanFolder(string folderPath)
        {
            string relativeFolder = Path.GetRelativePath(assetsPath, folderPath);
            string metadataPath = Path.Combine(folderPath, $"{Path.GetFileName(folderPath)}.divmeta");

            FolderMetadata folderMeta = new FolderMetadata { FolderPath = relativeFolder };
            foreach (string? file in Directory.GetFiles(folderPath))
            {
                if (!file.Contains(".divmeta"))
                {
                    Debug.Info($"Asset Database: Creating meta file for path (initial round):\n{file}");
                    AssetMetadata? asset = CreateAssetMetadata(file, relativeFolder);
                    folderMeta.Assets[Path.GetFileName(file)] = asset;
                }
            }

            folderMeta.LastScanTime = DateTime.Now;
            Folders[relativeFolder] = folderMeta;

            // Save folder metadata
            string folderJson = JsonSerializer.Serialize(folderMeta, jsonSerializerOptions);
            File.WriteAllText(metadataPath, folderJson);
        }

        /// <summary>
        /// Creates the asset metadata.
        /// </summary>
        /// <param name="filePath">File path of asset</param>
        /// <param name="relativeFolder">Folder relative to file path</param>
        /// <returns>Generated asset metadata</returns>
        private static AssetMetadata CreateAssetMetadata(string filePath, string relativeFolder)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return new AssetMetadata
            {
                FileName = Path.GetFileName(filePath),
                RelativePath = Path.GetRelativePath(relativeFolder, filePath),
                Type = DetermineAssetType(filePath),
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
            };
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

        // Queries

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
    }
}
