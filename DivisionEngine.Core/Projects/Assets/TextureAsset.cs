//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using SkiaSharp;

namespace DivisionEngine.Projects.Assets
{
    [AssetType(AssetType.Texture)]
    public class TextureAsset(AssetMetadata metadata) : Asset(metadata)
    {
        private byte[]? imageData;
        private int width;
        private int height;

        /// <summary>
        /// Raw image data (BGRA bytes).
        /// </summary>
        public byte[]? ImageData => imageData;

        public int Width => width;
        public int Height => height;

        public override async Task<bool> LoadAsync()
        {
            if (IsLoaded) return true;

            try
            {
                string fullPath = Path.Combine(AssetDatabase.ProjectPath, RelativePath);

                // Just load and decode to get dimensions, but store raw bytes
                using FileStream stream = File.OpenRead(fullPath);
                using SKBitmap bitmap = SKBitmap.Decode(stream) ?? throw new InvalidOperationException($"Failed to decode image: {Metadata.FileName}");
                width = bitmap.Width;
                height = bitmap.Height;

                // Reload to get raw bytes (or you could use the bitmap's bytes)
                imageData = await File.ReadAllBytesAsync(fullPath);

                IsLoaded = true;
                Debug.Info($"Texture loaded: {Metadata.FileName} ({width}x{height}, {imageData.Length} bytes)");
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
            imageData = null;
            width = 0;
            height = 0;
            IsLoaded = false;
            Debug.Info($"Texture unloaded: {Metadata.FileName}");
        }
    }
}
