#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine.Rendering
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct DenoiseShader(
        float width,
        float height,
        ReadOnlyTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadOnlyBuffer<SDFPrimitiveObjectDTO> sdfPrimitives,
        ReadOnlyBuffer<int> objectIdBuffer) : IComputeShader
    {
        // More forgiving thresholds for noisy reflections
        const float DEPTH_THRESHOLD = 0.2f;  // Increased from 0.1f
        const float NORMAL_THRESHOLD = 0.85f; // Relaxed from 0.9f
        const float MIN_ROUGHNESS_BLUR = 0.05f; // Start blurring earlier

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;

            // Get center pixel data
            float4 centerColor = inputTexture[pixel];
            float4 centerDepthNormal = depthNormals[pixel];
            float centerDepth = centerDepthNormal.X;
            float3 centerNormal = centerDepthNormal.YZW;
            int centerObjId = objectIdBuffer[pixel.X + pixel.Y * (int)width];

            // If no object hit, no blur needed
            if (centerObjId < 0)
            {
                outputTexture[pixel] = centerColor;
                return;
            }

            // Get roughness from material
            float roughness = sdfPrimitives[centerObjId].roughness;
            bool hasReflection = sdfPrimitives[centerObjId].hasReflection;

            // Skip blur if not reflective or very smooth
            if (!hasReflection || roughness < MIN_ROUGHNESS_BLUR)
            {
                outputTexture[pixel] = centerColor;
                return;
            }

            // More aggressive blur radius for rough surfaces
            // Scale: roughness 0.1 -> 2px, 0.5 -> 5px, 1.0 -> 8px
            int blurRadius = (int)Hlsl.Lerp(2.0f, 8.0f, roughness);

            float3 colorSum = float3.Zero;
            float weightSum = 0.0f;

            // Bilateral filter with adaptive parameters
            float spatialSigma = roughness * 3.0f + 0.5f; // Wider for rougher surfaces
            float depthSigma = DEPTH_THRESHOLD * (1.0f + roughness); // More forgiving for rough

            for (int dy = -blurRadius; dy <= blurRadius; dy++)
            {
                for (int dx = -blurRadius; dx <= blurRadius; dx++)
                {
                    int2 samplePixel = pixel + new int2(dx, dy);

                    // Bounds check
                    if (samplePixel.X < 0 || samplePixel.X >= (int)width ||
                        samplePixel.Y < 0 || samplePixel.Y >= (int)height)
                        continue;

                    float4 sampleDepthNormal = depthNormals[samplePixel];
                    float sampleDepth = sampleDepthNormal.X;
                    float3 sampleNormal = sampleDepthNormal.YZW;

                    // Depth similarity with adaptive threshold
                    float depthDiff = Hlsl.Abs(centerDepth - sampleDepth);
                    float depthWeight = Hlsl.Exp(-(depthDiff * depthDiff) / (2.0f * depthSigma * depthSigma));

                    // Skip if depth is too different (but more forgiving than before)
                    if (depthWeight < 0.1f) continue;

                    // Normal similarity with softer falloff
                    float normalSim = Hlsl.Max(0.0f, Hlsl.Dot(centerNormal, sampleNormal));
                    float normalWeight = Hlsl.Pow(normalSim, 2.0f); // Softer falloff than hard threshold

                    // Skip if normals are too different
                    if (normalSim < NORMAL_THRESHOLD) continue;

                    // Spatial weight (Gaussian)
                    float spatialDist = Hlsl.Sqrt((float)(dx * dx + dy * dy));
                    float spatialWeight = Hlsl.Exp(-(spatialDist * spatialDist) / (2.0f * spatialSigma * spatialSigma));

                    // Combine all weights
                    float weight = spatialWeight * depthWeight * normalWeight;

                    colorSum += inputTexture[samplePixel].XYZ * weight;
                    weightSum += weight;
                }
            }

            // Blend between original and blurred based on roughness
            float3 blurredColor = weightSum > 0.0f ? colorSum / weightSum : centerColor.XYZ;

            // Less blending for smoother surfaces, more for rough
            float blendFactor = Hlsl.SmoothStep(MIN_ROUGHNESS_BLUR, 0.7f, roughness);
            float3 finalColor = Hlsl.Lerp(centerColor.XYZ, blurredColor, blendFactor);

            outputTexture[pixel] = new float4(finalColor, 1.0f);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility