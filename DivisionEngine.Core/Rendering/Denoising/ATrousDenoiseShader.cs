#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine.Rendering.Denoising
{
    /// <summary>
    /// Computes A-Trous denoising on an image.
    /// </summary>
    /// <param name="width">Width of the image</param>
    /// <param name="height">Height of the image</param>
    /// <param name="stepSize">Step size for the denoiser</param>
    /// <param name="inputTexture">Input render</param>
    /// <param name="outputTexture">Output denoised render</param>
    /// <param name="depthNormals">Depth normal texture</param>
    /// <param name="sdfPrimitives">SDF primitives list</param>
    /// <param name="objectIdBuffer">Object ID Buffer</param>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct ATrousDenoiseShader(
        float width,
        float height,
        int stepSize,
        ReadWriteTexture2D<float4> inputTexture,
        ReadWriteTexture2D<float4> outputTexture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadOnlyBuffer<SDFPrimitiveObjectDTO> sdfPrimitives,
        ReadWriteBuffer<int> objectIdBuffer,
        ReadOnlyBuffer<float> kernelBuffer) : IComputeShader
    {

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            int centerObjId = objectIdBuffer[pixel.X + pixel.Y * (int)width];
            if (centerObjId < 0)
            {
                outputTexture[pixel] = inputTexture[pixel];
                return;
            }

            // Check reflections enabled
            float roughness = sdfPrimitives[centerObjId].roughness;
            if (sdfPrimitives[centerObjId].hasReflection == 0 || roughness < 0.05f)
            {
                outputTexture[pixel] = inputTexture[pixel];
                return;
            }

            float4 centerColor = inputTexture[pixel];
            float4 centerDepthNormal = depthNormals[pixel];
            float centerDepth = centerDepthNormal.X;
            float3 centerNormal = centerDepthNormal.YZW;

            float3 sumColor = float3.Zero;
            float sumWeight = 0f;

            // A-trous 5x5 filter with adaptive step size
            for (int yy = -2; yy <= 2; yy++)
            {
                for (int xx = -2; xx <= 2; xx++)
                {
                    int2 offset = new int2(xx * stepSize, yy * stepSize);
                    int2 samplePixel = pixel + offset;
                    if (samplePixel.X < 0 || samplePixel.X >= (int)width ||
                        samplePixel.Y < 0 || samplePixel.Y >= (int)height)
                        continue;

                    float4 sampleDepthNormal = depthNormals[samplePixel];

                    // Edge-stopping functions
                    float depthDiff = Hlsl.Abs(centerDepth - sampleDepthNormal.X);
                    float normalDiff = Hlsl.Max(0.0f, Hlsl.Dot(centerNormal, sampleDepthNormal.YZW));

                    // Weights
                    float spatialWeight = kernelBuffer[xx + 2] * kernelBuffer[yy + 2];
                    float depthWeight = Hlsl.Exp(-depthDiff * depthDiff / (2f * 0.1f * 0.1f));
                    float normalWeight = Hlsl.Pow(normalDiff, 32.0f / roughness);
                    float weight = spatialWeight * depthWeight * normalWeight;

                    sumColor += inputTexture[samplePixel].XYZ * weight;
                    sumWeight += weight;
                }
            }

            float3 finalColor = sumWeight > 0f ? sumColor / sumWeight : centerColor.XYZ;
            outputTexture[pixel] = new float4(finalColor, 1f);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility