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
    /// Darkens the edges of the screen with configurable radius.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct VignetteShader(
        float width,
        float height,
        float vignetteIntensity,
        float vignetteSmoothness,
        float vignetteRoundness,
        float vignetteRadius,
        float3 vignetteColor,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture) : IComputeShader
    {
        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            float2 uv = new float2(pixel.X / width, pixel.Y / height);
            float2 center = new float2(0.5f, 0.5f);
            float2 distVec = uv - center;

            // Calculate distance with roundness control
            float distance;
            if (vignetteRoundness >= 0.999f) distance = Hlsl.Length(distVec); // Perfect circle
            else if (vignetteRoundness <= 0.001f) distance = Hlsl.Max(Hlsl.Abs(distVec.X), Hlsl.Abs(distVec.Y)); // perfect rectangle
            else
            {
                float p = Hlsl.Lerp(8f, 2f, vignetteRoundness); // Higher p = more rectangular
                float2 absDist = Hlsl.Abs(distVec);
                distance = Hlsl.Pow(Hlsl.Pow(absDist.X, p) + Hlsl.Pow(absDist.Y, p), 1f / p);
            }

            // Max distance from center is ~0.707 for corners
            float maxDistance = 0.7071f;
            float scaledDistance = distance / (vignetteRadius * maxDistance);

            // Calculate vignette factor (1 at center, 0 at edges)
            float smoothness = Hlsl.Lerp(1f, 4f, vignetteSmoothness);
            float vignetteFactor = 1f - Hlsl.Pow(Hlsl.Saturate(scaledDistance), smoothness);
            vignetteFactor = Hlsl.Lerp(1f, vignetteFactor, vignetteIntensity);
            vignetteFactor = Hlsl.Clamp(vignetteFactor, 0f, 1f);

            // Apply vignette
            float3 color = inputTexture[pixel].XYZ;
            float3 darkenedColor = color * vignetteFactor;
            float3 finalColor = Hlsl.Lerp(darkenedColor, color * vignetteColor, (1f - vignetteFactor) * 0.5f);

            outputTexture[pixel] = new float4(finalColor, 1f);
        }
    }
}
