//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
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
        float width, float height, float focalDistance, float focalLength,
        float farPlaneDistance, float nearPlaneDistance, int maxBlurRadius,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> depthNormals) : IComputeShader
    {

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            float normalizedDepth = depthNormals[pixel].X;
            float worldDepth = Hlsl.Lerp(nearPlaneDistance, farPlaneDistance, normalizedDepth);
            float distanceFromFocal = Hlsl.Abs(worldDepth - focalDistance);
            float blurAmount = Hlsl.Saturate(distanceFromFocal / focalLength);
            float blurRadius = blurAmount * blurAmount * maxBlurRadius;

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