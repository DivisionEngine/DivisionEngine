//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Rendering;
using SkiaSharp;

namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Represents a texture file in a project.
    /// </summary>
    /// <param name="metadata">Asset metadata for this texture</param>
    [AssetType(AssetType.Texture)]
    public class TextureAsset(AssetMetadata metadata) : Asset(metadata)
    {
        private uint[]? pixelData;
        private int width;
        private int height;

        // Import settings (read from metadata)
        public TextureSampling Sampling { get; private set; } = TextureSampling.Bilinear;
        public TextureDimension Dimension { get; private set; } = TextureDimension.Texture2D;
        public int MaxMipmap { get; private set; } = 12;
        public CubemapLayout CubemapLayout { get; private set; } = CubemapLayout.None;

        public uint[]? PixelData => pixelData;
        public int Width => width;
        public int Height => height;

        public override async Task<bool> LoadAsync()
        {
            if (IsLoaded) return true;

            try
            {
                string fullPath = Path.Combine(AssetDatabase.ProjectPath, RelativePath);

                using FileStream stream = File.OpenRead(fullPath);
                SKBitmap bitmap = SKBitmap.Decode(stream) ?? throw new InvalidOperationException($"Failed to decode image: {Metadata.FileName}");

                width = bitmap.Width;
                height = bitmap.Height;
                SKColor[] skData = bitmap.Pixels;

                pixelData = new uint[skData.Length];
                for (int i = 0; i < skData.Length; i++)
                {
                    SKColor skPixel = skData[i];
                    pixelData[i] = (uint)((skPixel.Red << 24) | (skPixel.Green << 16) | (skPixel.Blue << 8) | skPixel.Alpha);
                }

                // Read custom properties from metadata
                Dictionary<string, object> props = Metadata.CustomProperties;
                if (props.TryGetValue("Sampling", out object? s))
                    Sampling = Enum.TryParse<TextureSampling>(s.ToString(), out var samp) ? samp : TextureSampling.Bilinear;

                if (props.TryGetValue("TextureType", out object? t))
                    Dimension = t.ToString() switch
                    {
                        "Texture3D" => TextureDimension.Texture3D,
                        "Cubemap" => TextureDimension.Cubemap,
                        _ => TextureDimension.Texture2D
                    };

                if (props.TryGetValue("MaxMipmap", out object? m) && int.TryParse(m.ToString(), out int mip))
                    MaxMipmap = Math.Clamp(mip, 0, 16);

                if (props.TryGetValue("CubemapLayout", out object? layoutObj) &&
                    Enum.TryParse<CubemapLayout>(layoutObj.ToString(), out var layout))
                    CubemapLayout = layout;

                // Force no mips for cubemaps
                if (Dimension == TextureDimension.Cubemap) MaxMipmap = 1;

                IsLoaded = true;
                Debug.Info($"Texture loaded: {Metadata.FileName} ({width}x{height}, {pixelData.Length} pixels)");
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
