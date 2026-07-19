//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;
using DivisionEngine.Rendering.ShaderUtilities;

namespace DivisionEngine.Rendering.Terrain
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct TerrainBakeShader(
            int2 resolution,
            float size,
            float heightScale,
            float frequency,
            int octaves,
            float lacunarity,
            float persistence,
            float ridgeBlend,
            float ridgeWeight,
            ReadWriteBuffer<float> output) : IComputeShader
    {
        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= resolution.X || pixel.Y >= resolution.Y) return;

            // Map texel to world-space XZ within [-size, size]
            float2 uv = (float2)pixel / new float2(resolution.X - 1, resolution.Y - 1) * 2f - 1f;
            float2 worldXZ = uv * size;

            // Sample simplex noise with FBM
            float noiseValue = SimplexNoise.FBM2D(worldXZ * frequency, octaves, persistence, lacunarity) * heightScale - heightScale / 2f;

            // Optional ridge noise blend
            //float ridgeValue = 0f;
            //if (ridgeWeight > 0f)
            //{
            //    ridgeValue = SimplexNoise.FBM2D(new float3(worldXZ * frequency, 0f), octaves);
            //    noiseValue = Hlsl.Lerp(noiseValue, ridgeValue, ridgeWeight);
            //}

            // Scale height: map from [0,1] to [-heightScale/2, heightScale/2]
            float height = (noiseValue - 0.5f) * heightScale;

            int index = pixel.Y * resolution.X + pixel.X;
            output[index] = height;
        }
    }
}
