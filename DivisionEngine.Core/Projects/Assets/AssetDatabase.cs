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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DivisionEngine.Projects.Assets
{
    public class AssetDatabase
    {
        private readonly string _assetsPath;
        private ProjectAssetDatabase _projectDb;
        private readonly FileSystemWatcher _fileWatcher;

        public AssetDatabase(string assetsPath)
        {
            _assetsPath = assetsPath;
            _projectDb = new ProjectAssetDatabase { ProjectPath = assetsPath };

            // Ensure Assets folder exists
            Directory.CreateDirectory(assetsPath);

            // Load existing database or create new
            LoadProjectDatabase();

            // Setup file watcher for real-time updates
            _fileWatcher = new FileSystemWatcher(assetsPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Deleted += OnFileDeleted;
            _fileWatcher.Renamed += OnFileRenamed;
        }

        public void ScanAllFolders()
        {
            foreach (var folder in Directory.GetDirectories(_assetsPath, "*", SearchOption.AllDirectories))
            {
                ScanFolder(folder);
            }
            // Also scan root
            ScanFolder(_assetsPath);

            SaveProjectDatabase();
        }

        private void ScanFolder(string folderPath)
        {
            string relativeFolder = Path.GetRelativePath(_assetsPath, folderPath);
            string metadataPath = Path.Combine(folderPath, ".divmeta");

            FolderMetadata folderMeta;

            if (File.Exists(metadataPath))
            {
                // Load existing metadata
                string json = File.ReadAllText(metadataPath);
                folderMeta = JsonSerializer.Deserialize<FolderMetadata>(json)
                    ?? new FolderMetadata { FolderPath = relativeFolder };

                // Check for deleted files
                var currentFiles = Directory.GetFiles(folderPath)
                    .Where(f => !Path.GetFileName(f).StartsWith('.'))
                    .ToDictionary(f => Path.GetFileName(f));

                foreach (var filename in folderMeta.Assets.Keys.ToList())
                {
                    if (!currentFiles.ContainsKey(filename))
                    {
                        // File was deleted
                        var asset = folderMeta.Assets[filename];
                        _projectDb.AllAssetsByGuid.Remove(asset.ID);
                        folderMeta.Assets.Remove(filename);
                    }
                }

                // Check for new/modified files
                foreach (var file in currentFiles)
                {
                    string fullPath = file.Value;
                    var lastModified = File.GetLastWriteTime(fullPath);

                    if (folderMeta.Assets.TryGetValue(file.Key, out var existingAsset))
                    {
                        // Update if modified
                        if (lastModified > existingAsset.LastModified)
                        {
                            UpdateAssetMetadata(existingAsset, fullPath);
                        }
                    }
                    else
                    {
                        // New asset
                        var newAsset = CreateAssetMetadata(fullPath, relativeFolder);
                        folderMeta.Assets[file.Key] = newAsset;
                        _projectDb.AllAssetsByGuid[newAsset.ID] = newAsset;
                    }
                }
            }
            else
            {
                // Create new folder metadata
                folderMeta = new FolderMetadata { FolderPath = relativeFolder };

                foreach (var file in Directory.GetFiles(folderPath)
                    .Where(f => !Path.GetFileName(f).StartsWith('.')))
                {
                    var asset = CreateAssetMetadata(file, relativeFolder);
                    folderMeta.Assets[Path.GetFileName(file)] = asset;
                    _projectDb.AllAssetsByGuid[asset.ID] = asset;
                }
            }

            folderMeta.LastScanTime = DateTime.Now;
            _projectDb.Folders[relativeFolder] = folderMeta;

            // Save folder metadata
            string folderJson = JsonSerializer.Serialize(folderMeta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(metadataPath, folderJson);
        }

        private AssetMetadata CreateAssetMetadata(string filePath, string relativeFolder)
        {
            var fileInfo = new FileInfo(filePath);
            return new AssetMetadata
            {
                FileName = Path.GetFileName(filePath),
                RelativePath = Path.Combine(relativeFolder, Path.GetFileName(filePath)).Replace('\\', '/'),
                Type = DetermineAssetType(filePath),
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length
            };
        }

        private void UpdateAssetMetadata(AssetMetadata metadata, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            metadata.LastModified = fileInfo.LastWriteTime;
            metadata.FileSize = fileInfo.Length;
            // Don't change GUID or other permanent properties
        }

        private AssetType DetermineAssetType(string filePath)
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

        private void LoadProjectDatabase()
        {
            string dbPath = Path.Combine(_assetsPath, "..", "ProjectAssets.divdb");
            if (File.Exists(dbPath))
            {
                string json = File.ReadAllText(dbPath);
                _projectDb = JsonSerializer.Deserialize<ProjectAssetDatabase>(json)
                    ?? new ProjectAssetDatabase { ProjectPath = _assetsPath };
            }
        }

        private void SaveProjectDatabase()
        {
            string dbPath = Path.Combine(_assetsPath, "..", "ProjectAssets.divdb");
            string json = JsonSerializer.Serialize(_projectDb, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dbPath, json);
        }

        // File watcher event handlers
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Queue a rescan of the affected folder
            string folder = Path.GetDirectoryName(e.FullPath)!;
            ScanFolder(folder);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            string folder = Path.GetDirectoryName(e.FullPath)!;
            ScanFolder(folder);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            string folder = Path.GetDirectoryName(e.FullPath)!;
            ScanFolder(folder);
        }

        // Query methods
        public AssetMetadata? GetAssetByGuid(string guid)
        {
            return _projectDb.AllAssetsByGuid.TryGetValue(guid, out var asset) ? asset : null;
        }

        public IEnumerable<AssetMetadata> GetAssetsInFolder(string relativeFolder)
        {
            return _projectDb.Folders.TryGetValue(relativeFolder, out var folder)
                ? folder.Assets.Values
                : Enumerable.Empty<AssetMetadata>();
        }

        public IEnumerable<AssetMetadata> GetAllAssets() => _projectDb.AllAssetsByGuid.Values;

        public IEnumerable<AssetMetadata> GetAssetsByType(AssetType type)
        {
            return _projectDb.AllAssetsByGuid.Values.Where(a => a.Type == type);
        }
    }
}
