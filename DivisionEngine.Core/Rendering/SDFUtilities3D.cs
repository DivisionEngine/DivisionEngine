using ComputeSharp;

#pragma warning disable CA1416 // Validate platform compatibility
namespace DivisionEngine
{

    /// <summary>
    /// Formats debug information from the depthNormals mask into an easier form to visualize.
    /// </summary>
    /// <param name="renderTex">Rendered output</param>
    /// <param name="depthNormals">Depth and normal information</param>
    /// <param name="debugMode">Debug mode to employ</param>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFDebug3D(
        ReadWriteTexture2D<float4> renderTex,
        ReadWriteTexture2D<float4> depthNormals,
        int debugMode) : IComputeShader
    {

        public void Execute()
        {
            int2 pixel = ThreadIds.XY; // Get pixel position
            if (debugMode == 1)
            {
                float depth = depthNormals[pixel].R;
                renderTex[pixel] = new float4(depth, depth, depth, 1); // Output visual depth buffer
            }
            else if (debugMode == 2)
                renderTex[pixel] = new float4(depthNormals[pixel].GBA, 1); // Output visual world normal buffer
            else renderTex[pixel] = new float4(0, 0, 0, 1); // Default path --> clear output
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
