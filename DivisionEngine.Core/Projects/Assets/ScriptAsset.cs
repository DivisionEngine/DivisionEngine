//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System.Text;

namespace DivisionEngine.Projects.Assets
{
    [AssetType(AssetType.Script)]
    public class ScriptAsset(AssetMetadata metadata) : Asset(metadata)
    {
        private string? _sourceCode;
        private DateTime _lastLoadedTime;

        /// <summary>
        /// Gets the source code of the script
        /// </summary>
        public string? SourceCode => _sourceCode;

        /// <summary>
        /// Gets the full path to the script file
        /// </summary>
        public string FullPath => Path.Combine(AssetDatabase.ProjectPath, RelativePath);

        public override async Task<bool> LoadAsync()
        {
            if (IsLoaded) return true;

            try
            {
                // Read the script file as text
                _sourceCode = await File.ReadAllTextAsync(FullPath, Encoding.UTF8);
                _lastLoadedTime = File.GetLastWriteTimeUtc(FullPath);

                IsLoaded = true;
                Debug.Info($"Script loaded: {Metadata.FileName} ({_sourceCode.Length} characters)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to load script {Metadata.FileName}: {ex.Message}");
                IsLoaded = false;
                _sourceCode = null;
                return false;
            }
        }

        /// <summary>
        /// Reloads the script if it has changed on disk
        /// </summary>
        public async Task<bool> ReloadIfChangedAsync()
        {
            if (!IsLoaded) return await LoadAsync();

            var lastWriteTime = File.GetLastWriteTimeUtc(FullPath);
            if (lastWriteTime > _lastLoadedTime)
            {
                Debug.Info($"Script changed on disk, reloading: {Metadata.FileName}");
                Unload();
                return await LoadAsync();
            }

            return true;
        }

        /// <summary>
        /// Gets the script content as a string
        /// </summary>
        public override string ToString() => _sourceCode ?? string.Empty;

        public override void Unload()
        {
            if (!IsLoaded) return;

            _sourceCode = null;
            IsLoaded = false;

            Debug.Info($"Script unloaded: {Metadata.FileName}");
        }
    }
}
