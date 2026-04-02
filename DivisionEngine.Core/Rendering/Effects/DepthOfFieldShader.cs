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
    /// Inaccurate to physical world, but visually pleasing Depth of Field shader.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct FastDepthOfFieldShader(
        float width,
        float height,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadWriteBuffer<uint2> objectIdBuffer,
        ReadOnlyBuffer<SDFObjectDTO> sdfObjBuffer,
        SDFWorldDTO worldDTO) : IComputeShader
    {

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            float normalizedDepth = depthNormals[pixel].X;
            float worldDepth = Hlsl.Lerp(worldDTO.nearPlane, worldDTO.farPlane, normalizedDepth);
            float distanceFromFocal = Hlsl.Abs(worldDepth - worldDTO.focusDistance);
            float blurAmount = Hlsl.Saturate(distanceFromFocal / worldDTO.focalLength);
            float blurRadius = blurAmount * blurAmount * 16f;

            if (blurRadius < 0.5f)
            {
                outputTexture[pixel] = inputTexture[pixel];
                return;
            }

            int radius = (int)Hlsl.Ceil(blurRadius);
            float3 sumColor = float3.Zero;
            float totalWeight = 0f;
            for (int yy = -radius; yy <= radius; yy++)
            {
                for (int xx = -radius; xx <= radius; xx++)
                {
                    int2 samplePixel = pixel + new int2(xx, yy);
                    if (samplePixel.X < 0 || samplePixel.X >= (int)width ||
                        samplePixel.Y < 0 || samplePixel.Y >= (int)height) continue;
                    float distance = Hlsl.Sqrt(xx * xx + yy * yy);
                    if (distance > blurRadius) continue;

                    // Simple inverse distance weight
                    float weight = 1f / (1f + distance);
                    sumColor += inputTexture[samplePixel].XYZ * weight;
                    totalWeight += weight;
                }
            }

            float3 finalColor = totalWeight > 0f ? sumColor / totalWeight : inputTexture[pixel].XYZ;
            outputTexture[pixel] = new float4(finalColor, 1f);
        }
    }
}