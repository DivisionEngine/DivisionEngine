//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine.Rendering
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct DivisionDenoiseShader(
        float width,
        float height,
        float divisionDenoise,
        int divisionDomain,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadOnlyBuffer<SDFObjectDTO> sdfObjects,
        ReadWriteBuffer<uint2> objectIdBuffer) : IComputeShader
    {
        // More forgiving thresholds for noisy reflections
        const float DEPTH_THRESHOLD = 0.2f;  // Increased from 0.1f
        const float NORMAL_THRESHOLD = 0.85f; // Relaxed from 0.9f
        const float MIN_ROUGHNESS_BLUR = 0.05f; // Start blurring earlier

        // Use a parabola that you find using a parabolic regression to change the threshold over time in the future!
        public float3 DivisionDenoise(float3 center, int2 pixel, float roughness)
        {
            float3 blurred = float3.Zero;
            int total = 0;
            for (int x = -divisionDomain; x <= divisionDomain; x++)
            {
                for (int y = -divisionDomain; y <= divisionDomain; y++)
                {
                    if (x == 0 && y == 0) continue;
                    blurred += inputTexture[pixel + new int2(x, y)].RGB;
                    total += 1;
                }
            }

            blurred /= total;
            if (Hlsl.Distance(blurred, center) > Hlsl.Max(1f - roughness - divisionDenoise, 0f)) return blurred;
            return center;
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;

            // Get center pixel data
            float4 centerColor = inputTexture[pixel];
            float4 centerDepthNormal = depthNormals[pixel];
            float centerDepth = centerDepthNormal.X;
            float3 centerNormal = centerDepthNormal.YZW;
            int centerObjId = (int)objectIdBuffer[pixel.X + pixel.Y * (int)width].X;

            // If no object hit, no blur needed
            if (centerObjId < 0)
            {
                outputTexture[pixel] = centerColor;
                return;
            }

            // Get roughness from material
            float roughness = sdfObjects[centerObjId].roughness;

            // Skip blur if not reflective or very smooth
            if (sdfObjects[centerObjId].hasReflection == 0 || roughness < MIN_ROUGHNESS_BLUR)
            {
                outputTexture[pixel] = centerColor;
                return;
            }

            // Perform custom Division Denoising
            if (pixel.X > divisionDomain - 1 && pixel.Y > divisionDomain - 1 &&
                pixel.X < width - divisionDomain && pixel.Y < height - divisionDomain)
                centerColor.RGB = DivisionDenoise(centerColor.RGB, pixel, roughness);
            outputTexture[pixel] = centerColor;

            // More aggressive blur radius for rough surfaces
            int blurRadius = (int)Hlsl.Lerp(2.0f, 4.0f, roughness);

            float3 colorSum = float3.Zero;
            float weightSum = 0f;

            // Bilateral filter with adaptive parameters
            float spatialSigma = roughness * 3f + 0.5f; // Wider for rougher surfaces
            float depthSigma = DEPTH_THRESHOLD * (1f + roughness); // More forgiving for rough
            for (int dy = -blurRadius; dy <= blurRadius; dy++)
            {
                for (int dx = -blurRadius; dx <= blurRadius; dx++)
                {
                    int2 samplePixel = pixel + new int2(dx, dy);
                    if (samplePixel.X < 0 || samplePixel.X >= (int)width ||
                        samplePixel.Y < 0 || samplePixel.Y >= (int)height)
                        continue;
                    float4 sampleDepthNormal = depthNormals[samplePixel];

                    // Depth similarity with adaptive threshold
                    float depthDiff = Hlsl.Abs(centerDepth - sampleDepthNormal.X);
                    float depthWeight = Hlsl.Exp(-(depthDiff * depthDiff) / (2f * depthSigma * depthSigma));

                    // Skip if depth is too different (but more forgiving than before)
                    if (depthWeight < 0.1f) continue;

                    // Normal similarity with softer falloff
                    float normalSim = Hlsl.Max(0f, Hlsl.Dot(centerNormal, sampleDepthNormal.YZW));
                    float normalWeight = Hlsl.Pow(normalSim, 3f); // Softer falloff than hard threshold

                    // Skip if normals are too different
                    if (normalSim < NORMAL_THRESHOLD) continue;
                    float spatialDist = Hlsl.Sqrt(dx * dx + dy * dy); // Spatial weight (Gaussian)
                    float spatialWeight = Hlsl.Exp(-(spatialDist * spatialDist) / (2f * spatialSigma * spatialSigma));

                    // Combine weights
                    float weight = spatialWeight * depthWeight * normalWeight;
                    colorSum += inputTexture[samplePixel].XYZ * weight;
                    weightSum += weight;
                }
            }

            // Blend between original and blurred based on roughness
            float3 blurredColor = weightSum > 0f ? colorSum / weightSum : centerColor.XYZ;

            // Less blending for smoother surfaces, more for rough
            float blendFactor = Hlsl.SmoothStep(MIN_ROUGHNESS_BLUR, 0.7f, roughness);
            float3 finalColor = Hlsl.Lerp(centerColor.XYZ, blurredColor, blendFactor);

            outputTexture[pixel] = new float4(finalColor, 1f);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility