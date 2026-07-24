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
    /// Two-pass separable Gaussian blur for better performance.
    /// </summary>
    /// <remarks>Pass 0 = Horizontal, Pass 1 = Vertical</remarks>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct TwoPassBlurShader(
        float width,
        float height,
        float blurRadius,
        int pass, // 0 = horizontal, 1 = vertical
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture) : IComputeShader
    {
        private static float GaussianWeight(float x, float sigma)
        {
            float sigma2 = sigma * sigma;
            return 1f / Hlsl.Sqrt(2f * 3.141592654f * sigma2) * Hlsl.Exp(-(x * x) / (2f * sigma2));
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            float sigma = Hlsl.Max(blurRadius * 0.5f, 0.5f);
            int kernelSize = (int)Hlsl.Ceil(sigma * 3f);
            kernelSize = Hlsl.Max(kernelSize, 1);

            float4 result = float4.Zero;
            float totalWeight = 0f;

            // Horizontal or vertical pass
            for (int i = -kernelSize; i <= kernelSize; i++)
            {
                int2 samplePos = pass == 0
                    ? pixel + new int2(i, 0)
                    : pixel + new int2(0, i);

                if (samplePos.X < 0 || samplePos.X >= (int)width ||
                    samplePos.Y < 0 || samplePos.Y >= (int)height)
                    continue;

                float weight = GaussianWeight(i, sigma);
                result += inputTexture[samplePos] * weight;
                totalWeight += weight;
            }

            outputTexture[pixel] = totalWeight > 0f ? result / totalWeight : inputTexture[pixel];
        }
    }
}
