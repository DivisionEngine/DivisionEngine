//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects.Assets
{
    [AssetType(AssetType.Audio)]
    public class AudioAsset(AssetMetadata metadata) : Asset(metadata)
    {
        private byte[]? audioData;

        /// <summary>
        /// Raw audio file data (WAV/MP3/OGG bytes).
        /// </summary>
        public byte[]? AudioData => audioData;

        public override async Task<bool> LoadAsync()
        {
            if (IsLoaded) return true;

            try
            {
                string fullPath = Path.Combine(AssetDatabase.ProjectPath, RelativePath);
                audioData = await File.ReadAllBytesAsync(fullPath);

                IsLoaded = true;
                Debug.Info($"Audio loaded: {Metadata.FileName} ({audioData.Length} bytes)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to load audio {Metadata.FileName}: {ex.Message}");
                IsLoaded = false;
                return false;
            }
        }

        public override void Unload()
        {
            audioData = null;
            IsLoaded = false;
            Debug.Info($"Audio unloaded: {Metadata.FileName}");
        }
    }
}
