//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects.Assets
{
    [AssetType(AssetType.Texture)]
    public class TextureAsset(AssetMetadata metadata) : Asset(metadata)
    {
        // Texture-specific properties
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MipLevels { get; private set; }

        // Runtime texture handle (would be whatever your renderer uses)
        private object? _textureHandle;

        public override async Task<bool> LoadAsync()
        {
            try
            {
                // Simulate loading (replace with actual texture loading)
                await Task.Delay(100);

                // For demo purposes, set some fake dimensions
                Width = 512;
                Height = 512;
                MipLevels = 1;

                // In reality, you'd load the texture data here
                // _textureHandle = await LoadTextureFromFile(GetFullPath());

                IsLoaded = true;
                Debug.Info($"Texture loaded: {Metadata.FileName} ({Width}x{Height})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to load texture {Metadata.FileName}: {ex.Message}");
                IsLoaded = false;
                return false;
            }
        }

        public override void Unload()
        {
            if (!IsLoaded) return;

            // Unload texture (dispose handle, etc.)
            // _textureHandle?.Dispose();
            // _textureHandle = null;

            IsLoaded = false;
            Debug.Info($"Texture unloaded: {Metadata.FileName}");
        }

        // Helper to get full path (you might want to inject AssetDatabase)
        private string GetFullPath()
        {
            // This would need the assetsPath from somewhere
            // For now, just return relative path
            return Metadata.RelativePath;
        }
    }
}
