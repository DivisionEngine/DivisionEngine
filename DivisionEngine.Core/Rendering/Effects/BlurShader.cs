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
    /// Gaussian blur post-processing effect.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct BlurShader(
        float width,
        float height,
        float blurRadius,
        int blurPasses,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> tempTexture) : IComputeShader
    {
        /// <summary>
        /// Gaussian blur kernel weights for 5x5 kernel.
        /// </summary>
        private static float GaussianWeight(float x, float sigma)
        {
            float sigma2 = sigma * sigma;
            return 1f / Hlsl.Sqrt(2f * 3.141592654f * sigma2) * Hlsl.Exp(-(x * x) / (2f * sigma2));
        }

        /// <summary>
        /// Applies a 1D Gaussian blur along the X axis.
        /// </summary>
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
                result += inputTexture[samplePos] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0f ? result / totalWeight : inputTexture[pixel];
        }

        /// <summary>
        /// Applies a 1D Gaussian blur along the Y axis.
        /// </summary>
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
                result += inputTexture[samplePos] * weight;
                totalWeight += weight;
            }

            return totalWeight > 0f ? result / totalWeight : inputTexture[pixel];
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            float sigma = Hlsl.Max(blurRadius * 0.5f, 0.5f);
            float4 finalColor = inputTexture[pixel];

            // Apply multiple passes for stronger blur
            for (int pass = 0; pass < blurPasses; pass++)
            {
                // Horizontal pass
                float4 horizontal = BlurHorizontal(pixel, sigma);

                // Vertical pass (using the horizontal result)
                if (pass < blurPasses - 1) tempTexture[pixel] = horizontal;

                // For the final pass, compute vertical directly
                if (pass == blurPasses - 1) finalColor = BlurVertical(pixel, sigma);
                else tempTexture[pixel] = horizontal;
            }

            outputTexture[pixel] = finalColor;
        }
    }
}
