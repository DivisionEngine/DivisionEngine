#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine.Rendering.Effects
{
    /// <summary>
    /// Vignette post-processing effect.
    /// Darkens the edges of the screen.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct VignetteShader(
        float width,
        float height,
        float vignetteIntensity,   // 0.0 - 1.0, how strong the effect is
        float vignetteSmoothness,  // 0.0 - 1.0, how smooth the transition is
        float vignetteRoundness,   // 0.0 - 1.0, roundness of the vignette (0 = rectangular, 1 = circular)
        float3 vignetteColor,      // Color of the vignette (black by default)
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture) : IComputeShader
    {
        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            if (pixel.X >= width || pixel.Y >= height) return;

            // Calculate UV coordinates (0-1 range)
            float2 uv = new float2(pixel.X / width, pixel.Y / height);

            // Calculate distance from center with optional roundness
            float2 center = new float2(0.5f, 0.5f);
            float2 distVec = uv - center;

            // Apply roundness (1 = circular, 0 = rectangular)
            float2 distVecRounded = distVec;
            if (vignetteRoundness < 1.0f)
            {
                // Blend between circular and rectangular
                float r = Hlsl.Length(distVec);
                float rect = Hlsl.Max(Hlsl.Abs(distVec.X), Hlsl.Abs(distVec.Y));
                float blend = Hlsl.Lerp(rect, r, vignetteRoundness);
                distVecRounded = new float2(blend, blend);
            }

            float distance = Hlsl.Length(distVecRounded);

            // Calculate vignette factor (1 at center, 0 at edges)
            // Smoothness controls the falloff curve
            float smoothness = Hlsl.Lerp(1.0f, 3.0f, vignetteSmoothness);
            float vignetteFactor = 1.0f - Hlsl.Pow(distance * 2.0f, smoothness);
            vignetteFactor = Hlsl.Clamp(vignetteFactor, 0.0f, 1.0f);

            // Apply intensity
            vignetteFactor = Hlsl.Lerp(1.0f, vignetteFactor, vignetteIntensity);

            // Apply vignette color (black by default)
            float3 color = inputTexture[pixel].XYZ;
            float3 darkenedColor = color * vignetteFactor;

            // Blend with vignette color for artistic effect
            float3 finalColor = Hlsl.Lerp(darkenedColor, vignetteColor * color, (1.0f - vignetteFactor) * 0.5f);

            outputTexture[pixel] = new float4(finalColor, 1.0f);
        }
    }
}
