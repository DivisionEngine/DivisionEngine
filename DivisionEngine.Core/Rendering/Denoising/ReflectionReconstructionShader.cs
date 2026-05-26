//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine.Rendering.Denoising
{
    /// <summary>
    /// Smart reconstruction for incomplete ray-traced reflections
    /// Tracks incomplete rays and replaces them with nearby complete samples
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct ReflectionReconstructionShader(
        float width,
        float height,
        float minBounceThreshold, // Minimum bounces to consider "complete"
        float reconstructionRadius,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<int> bounceCountTexture,
        ReadWriteTexture2D<float4> depthNormals) : IComputeShader
    {
        // First, we need to modify the rendering shader to output bounce counts
        // Add this to SDFShader3D's TraceRayWithReflections:
        // - Store bounce count in a separate texture
        // OR compute "completeness" as: (actual bounces / max possible bounces)

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;

            // Get bounce count for this pixel
            int centerBounces = bounceCountTexture[pixel];
            float4 centerColor = inputTexture[pixel];
            float4 centerDepthNormal = depthNormals[pixel];
            float centerDepth = centerDepthNormal.X;
            float3 centerNormal = centerDepthNormal.YZW;

            // Check if this pixel has "complete" reflection data
            bool isComplete = centerBounces >= minBounceThreshold;

            // If complete, just pass through (or do minimal denoising)
            if (isComplete)
            {
                // Optionally do very light edge-aware filtering for complete pixels
                outputTexture[pixel] = centerColor;
                return;
            }

            // Incomplete pixel: Need to reconstruct from nearby complete pixels
            float3 colorSum = float3.Zero;
            float weightSum = 0f;
            int completeSamplesFound = 0;

            // Search radius for reconstruction (larger for more incomplete pixels)
            int searchRadius = (int)reconstructionRadius;

            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                for (int dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    int2 samplePixel = pixel + new int2(dx, dy);

                    // Skip out of bounds
                    if (samplePixel.X < 0 || samplePixel.X >= (int)width ||
                        samplePixel.Y < 0 || samplePixel.Y >= (int)height)
                        continue;

                    // Skip the center pixel (we know it's incomplete)
                    if (dx == 0 && dy == 0) continue;

                    int sampleBounces = bounceCountTexture[samplePixel];

                    // Only use "complete" samples for reconstruction
                    if (sampleBounces < minBounceThreshold) continue;

                    completeSamplesFound++;

                    float4 sampleDepthNormal = depthNormals[samplePixel];

                    // Calculate reconstruction weights
                    // 1. Spatial distance weight
                    float spatialDist = Hlsl.Length(new float2(dx, dy));
                    float spatialWeight = Hlsl.Exp(-spatialDist * spatialDist / (2f * reconstructionRadius * reconstructionRadius));

                    // 2. Depth similarity
                    float depthDiff = Hlsl.Abs(centerDepth - sampleDepthNormal.X);
                    float depthWeight = Hlsl.Exp(-depthDiff * depthDiff / 0.01f);

                    // 3. Normal similarity
                    float3 sampleNormal = sampleDepthNormal.YZW;
                    float normalSim = Hlsl.Max(0f, Hlsl.Dot(centerNormal, sampleNormal));
                    float normalWeight = Hlsl.Pow(normalSim, 8f);

                    // Combine weights
                    float weight = spatialWeight * depthWeight * normalWeight;

                    colorSum += inputTexture[samplePixel].XYZ * weight;
                    weightSum += weight;
                }
            }

            // Reconstruction result
            if (completeSamplesFound > 0 && weightSum > 0f)
            {
                // We have complete neighbors to reconstruct from
                float3 reconstructedColor = colorSum / weightSum;

                // Blend based on how many complete samples we found
                float confidence = Hlsl.Clamp(completeSamplesFound / (float)(searchRadius * searchRadius * 2f), 0f, 1f);
                float3 finalColor = Hlsl.Lerp(centerColor.XYZ, reconstructedColor, confidence);

                outputTexture[pixel] = new float4(finalColor, 1f);
            }
            else
            {
                // No complete neighbors found - fall back to standard bilateral filter
                // Or keep original (noisy) color
                outputTexture[pixel] = centerColor;
            }
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility