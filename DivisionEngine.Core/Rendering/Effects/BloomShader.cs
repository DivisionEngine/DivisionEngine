//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;
using DivisionEngine.Rendering.ShaderUtilities;

namespace DivisionEngine.Rendering.Effects
{
    /// <summary>
    /// Bloom effect using bright pass extraction and Gaussian blur.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct BloomShader(
        float width,
        float height,
        float threshold,
        float knee,
        float intensity,
        float blurRadius,
        int blurPasses,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> brightTexture,
        ReadWriteTexture2D<float4> blurTempTexture) : IComputeShader
    {
        private float4 BrightPass(float4 color)
        {
            float3 rgb = color.RGB;
            float luminance = ShaderMath.Luminance(rgb);

            // Soft knee threshold
            float knee2 = knee * 2f;
            float bright = luminance - threshold;
            bright = Hlsl.Max(bright, 0f);
            bright = (bright * bright) / (bright + knee2);
            bright = Hlsl.Clamp(bright, 0f, 1f);

            return new float4(rgb * bright, color.W);
        }

        private float4 BlurHorizontal(int2 pixel, float sigma)
        {
            int kernelSize = (int)Hlsl.Ceil(sigma * 3f);
            kernelSize = Hlsl.Max(kernelSize, 1);

            float4 result = float4.Zero;
            float totalWeight = 0f;
            for (int x = -kernelSize; x <= kernelSize; x++)
            {
                int2 samplePos = pixel + new int2(x, 0);
                if (samplePos.X < 0 || samplePos.X >= (int)width ||
                    samplePos.Y < 0 || samplePos.Y >= (int)height)
                    continue;

                float weight = GaussianWeight(x, sigma);
                result += brightTexture[samplePos] * weight;
                totalWeight += weight;
            }
            return totalWeight > 0f ? result / totalWeight : brightTexture[pixel];
        }

        private float4 BlurVertical(int2 pixel, float sigma)
        {
            int kernelSize = (int)Hlsl.Ceil(sigma * 3f);
            kernelSize = Hlsl.Max(kernelSize, 1);

            float4 result = float4.Zero;
            float totalWeight = 0f;
            for (int y = -kernelSize; y <= kernelSize; y++)
            {
                int2 samplePos = pixel + new int2(0, y);
                if (samplePos.X < 0 || samplePos.X >= (int)width ||
                    samplePos.Y < 0 || samplePos.Y >= (int)height)
                    continue;

                float weight = GaussianWeight(y, sigma); 
                result += blurTempTexture[samplePos] * weight; // Use horizontal-blurred result from temp texture
                totalWeight += weight;
            }
            return totalWeight > 0f ? result / totalWeight : blurTempTexture[pixel];
        }

        private static float GaussianWeight(float x, float sigma)
        {
            float sigma2 = sigma * sigma;
            return 1f / Hlsl.Sqrt(2f * 3.141592654f * sigma2) * Hlsl.Exp(-(x * x) / (2f * sigma2));
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            float4 originalColor = inputTexture[pixel];
            float4 brightColor = BrightPass(originalColor); // Extract bright regions
            brightTexture[pixel] = brightColor;
            float sigma = Hlsl.Max(blurRadius * 0.5f, 0.5f); // Blur the bright texture
            float4 blurred = brightColor;

            // Blur passes
            for (int pass = 0; pass < blurPasses; pass++)
            {
                float4 horizontal = BlurHorizontal(pixel, sigma);
                blurTempTexture[pixel] = horizontal;
                float4 vertical = BlurVertical(pixel, sigma);
                blurred = vertical;

                // Update brightTexture for next pass
                brightTexture[pixel] = blurred;
            }

            // Combine with original
            float3 combined = originalColor.RGB + (blurred.RGB * intensity);
            outputTexture[pixel] = new float4(combined, originalColor.W);
        }
    }
}
