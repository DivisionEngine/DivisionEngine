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

                //SKImageInfo imageDecoder = new SKImageInfo()
                //{
                //    ColorSpace = SKColorSpace.CreateSrgbLinear(),
                //};
                FileStream stream = File.OpenRead(fullPath);
                SKBitmap bitmap = SKBitmap.Decode(stream) ?? throw new InvalidOperationException($"Failed to decode image: {Metadata.FileName}");

                width = bitmap.Width;
                height = bitmap.Height;
                SKColor[] skData = bitmap.Pixels;

                // Convert BGRA bytes to float4 (RGBA, normalized to 0-1)
                pixelData = new float4[skData.Length];
                for (int i = 0; i < skData.Length; i++)
                {
                    SKColor skPixel = skData[i];
                    pixelData[i] = new float4(
                        skPixel.Red / 255.0f,
                        skPixel.Green / 255.0f,
                        skPixel.Blue / 255.0f,
                        skPixel.Alpha / 255.0f
                    );
                }

                stream.Dispose();
                bitmap.Dispose();
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
