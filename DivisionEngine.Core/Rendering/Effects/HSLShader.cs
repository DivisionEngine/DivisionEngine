//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine.Rendering.Effects
{
    /// <summary>
    /// Hue, Saturation, and Lightness color grading effect.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct HSLShader(
        float width,
        float height,
        float hueShift,        // -180 to 180 degrees
        float saturation,      // 0 to 2 (1 = normal)
        float lightness,       // 0 to 2 (1 = normal)
        float contrast,        // 0 to 1 (1 = normal)
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture) : IComputeShader
    {
        /// <summary>
        /// Converts RGB to HSL.
        /// </summary>
        private static float3 RgbToHsl(float3 rgb)
        {
            float max = Hlsl.Max(rgb.X, Hlsl.Max(rgb.Y, rgb.Z));
            float min = Hlsl.Min(rgb.X, Hlsl.Min(rgb.Y, rgb.Z));
            float delta = max - min;

            float h = 0f;
            float s = 0f;
            float l = (max + min) * 0.5f;

            if (delta > 0.001f)
            {
                s = l < 0.5f ? delta / (max + min) : delta / (2f - max - min);

                if (max == rgb.X)
                    h = (rgb.Y - rgb.Z) / delta + (rgb.Y < rgb.Z ? 6f : 0f);
                else if (max == rgb.Y)
                    h = (rgb.Z - rgb.X) / delta + 2f;
                else // max == rgb.Z
                    h = (rgb.X - rgb.Y) / delta + 4f;

                h /= 6f;
            }

            return new float3(h, s, l);
        }

        private static float Hue2RGB(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 0.5f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        /// <summary>
        /// Converts HSL to RGB.
        /// </summary>
        private static float3 HslToRgb(float3 hsl)
        {
            float h = hsl.X;
            float s = hsl.Y;
            float l = hsl.Z;

            if (s < 0.001f) return new float3(l, l, l);

            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;

            return new float3(
                Hue2RGB(p, q, h + 1f / 3f),
                Hue2RGB(p, q, h),
                Hue2RGB(p, q, h - 1f / 3f)
            );
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            float4 color = inputTexture[pixel];
            float3 hsl = RgbToHsl(color.RGB); // Convert to HSL

            // Apply adjustments
            hsl.X = Hlsl.Frac(hsl.X + hueShift / 360f); // Hue shift in degrees
            hsl.Y = Hlsl.Clamp(hsl.Y * saturation, 0f, 1f);
            hsl.Z = Hlsl.Clamp(hsl.Z * lightness, 0f, 1f);
            float3 rgb = HslToRgb(hsl); // Convert back to RGB
            float3 contrastColor = (rgb - 0.5f) * contrast + 0.5f; // Apply contrast
            contrastColor = Hlsl.Clamp(contrastColor, 0f, 1f);

            outputTexture[pixel] = new float4(contrastColor, color.W);
        }
    }
}
