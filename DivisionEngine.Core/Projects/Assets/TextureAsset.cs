//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using SkiaSharp;
using System.Runtime.InteropServices;

namespace DivisionEngine.Projects.Assets
{
    [AssetType(AssetType.Texture)]
    public class TextureAsset(AssetMetadata metadata) : Asset(metadata)
    {
        private float4[]? pixelData;
        private int width;
        private int height;

        /// <summary>
        /// Raw image data.
        /// </summary>
        public float4[]? PixelData => pixelData;

        public int Width => width;
        public int Height => height;

        public override async Task<bool> LoadAsync()
        {
            if (IsLoaded) return true;

            try
            {
                string fullPath = Path.Combine(AssetDatabase.ProjectPath, RelativePath);

                using FileStream stream = File.OpenRead(fullPath);
                using SKBitmap bitmap = SKBitmap.Decode(stream) ?? throw new InvalidOperationException($"Failed to decode image: {Metadata.FileName}");

                width = bitmap.Width;
                height = bitmap.Height;

                // Get raw pixel data (BGRA format)
                IntPtr pixelPtr = bitmap.GetPixels();
                int pixelCount = width * height;
                byte[] bgraData = new byte[pixelCount * 4];
                Marshal.Copy(pixelPtr, bgraData, 0, bgraData.Length);

                // Convert BGRA bytes to float4 (RGBA, normalized to 0-1)
                pixelData = new float4[pixelCount];
                for (int i = 0; i < pixelCount; i++)
                {
                    int offset = i * 4;
                    pixelData[i] = new float4(
                        bgraData[offset + 2] / 255.0f, // R
                        bgraData[offset + 1] / 255.0f, // G
                        bgraData[offset + 0] / 255.0f, // B
                        bgraData[offset + 3] / 255.0f  // A
                    );
                }

                IsLoaded = true;
                Debug.Info($"Texture loaded: {Metadata.FileName} ({width}x{height}, {pixelData.Length} bytes)");
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
            pixelData = null;
            width = 0;
            height = 0;
            IsLoaded = false;
            Debug.Info($"Texture unloaded: {Metadata.FileName}");
        }
    }
}
