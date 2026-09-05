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
    /// Very basic implmentation of chromatic aberration (has some artifacts for now).
    /// </summary>
    /// <param name="width">Width of input texture</param>
    /// <param name="height">Height of input texture</param>
    /// <param name="intensity">Intensity of chromatic aberration effect</param>
    /// <param name="inputTexture">Input render texture</param>
    /// <param name="outputTexture">Output texture with aberration</param>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct ChromaticAberrationShader(
        float width,
        float height,
        float intensity,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture) : IComputeShader
    {
        public void Execute()
        {
            int2 pixel = ThreadIds.XY;

            float2 uv = new float2(pixel.X / width, pixel.Y / height);
            float2 center = new float2(0.5f, 0.5f);
            float distance = Hlsl.Distance(center, uv);

            float2 rUV = uv - new float2(-0.5f, 0.5f) * distance * intensity;
            float2 gUV = uv - new float2(0.7f, -0.6f) * distance * intensity;
            float2 bUV = uv - new float2(-0.3f, 0.4f) * distance * intensity;

            float r = inputTexture[new int2((int)(rUV.X * width), (int)(rUV.Y * height))].R;
            float g = inputTexture[new int2((int)(gUV.X * width), (int)(gUV.Y * height))].G;
            float b = inputTexture[new int2((int)(bUV.X * width), (int)(bUV.Y * height))].B;

            outputTexture[pixel] = new float4(r, g, b, 1f);
        }
    }
}
