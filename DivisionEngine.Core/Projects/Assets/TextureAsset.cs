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
namespace DivisionEngine.Projects.Assets
{
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
