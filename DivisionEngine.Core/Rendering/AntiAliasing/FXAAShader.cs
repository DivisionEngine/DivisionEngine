//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;

namespace DivisionEngine.Rendering.AntiAliasing
{
    /// <summary>
    /// Computes fast approximate anti-aliasing on an image.
    /// </summary>
    /// <param name="width">Width of image</param>
    /// <param name="height">Height of image</param>
    /// <param name="inputTexture">Input image</param>
    /// <param name="outputTexture">Anti-aliased output image</param>
    /// <param name="edgeThreshold">Threshold for edge detection</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(8, 8, 1)]
    public readonly partial struct FXAAShader(
        int width,
        int height,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        float edgeThreshold = 0.05f,
        float strength = 0.5f,
        int area = 2,
        int debug = 0) : IComputeShader
    {
        private static float Luminance(float4 color)
        {
            return color.R * 0.299f + color.G * 0.587f + color.B * 0.114f;
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            float4 center = inputTexture[pixel];
            float centerLuma = Luminance(center);

            // Sample neighbors
            int count = 0;
            float4 finalColor = float4.Zero;
            for (int x = -area; x <= area; x++)
            {
                for (int y = -area; y <= area; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        int2 curPix = new int2(x + pixel.X, y + pixel.Y);
                        finalColor += inputTexture[curPix];
                        count++;
                    }
                }
            }

            finalColor /= count;
            float4 left = inputTexture[new int2(pixel.X - 1, pixel.Y)];
            float4 right = inputTexture[new int2(pixel.X + 1, pixel.Y)];
            float4 up = inputTexture[new int2(pixel.X, pixel.Y - 1)];
            float4 down = inputTexture[new int2(pixel.X, pixel.Y + 1)];

            float lumaLeft = Luminance(left);
            float lumaRight = Luminance(right);
            float lumaUp = Luminance(up);
            float lumaDown = Luminance(down);

            // Detect edge
            float lumaMin = Hlsl.Min(Hlsl.Min(lumaLeft, lumaRight), Hlsl.Min(lumaUp, lumaDown));
            float lumaMax = Hlsl.Max(Hlsl.Max(lumaLeft, lumaRight), Hlsl.Max(lumaUp, lumaDown));

            // If no edge, output original
            if (lumaMax - lumaMin < edgeThreshold)
            {
                outputTexture[pixel] = center;
                return;
            }

            // Simple blur along edge (2x2 average)
            //float4 avgColor = (left + right + up + down + center) / 5f;

            // Blend based on edge strength
            float edgeStrength = Hlsl.Saturate((lumaMax - lumaMin) / edgeThreshold);
            float4 result = Hlsl.Lerp(center, finalColor, edgeStrength * strength);

            if (debug == 0) outputTexture[pixel] = result;
            else if (debug == 1)
                outputTexture[pixel] = new float4(1f - result.R, 1f - result.G, result.B, result.A);
            else if (debug == 2)
            {
                // Show edge direction (color coded)
                float lumaX = lumaLeft - lumaRight;
                float lumaY = lumaUp - lumaDown;
                float2 normDir = Hlsl.Normalize(new float2(lumaX, lumaY));
                float3 dirColor = new float3(Hlsl.Abs(normDir.X), Hlsl.Abs(normDir.Y), 0f);
                dirColor = Hlsl.Saturate(dirColor * 2f);
                outputTexture[pixel] = new float4(dirColor, 1f);
            }
            else outputTexture[pixel] = result;
        }
    }
}
