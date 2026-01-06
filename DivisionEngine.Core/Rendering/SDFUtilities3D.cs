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
        ReadWriteBuffer<int> objectIdBuffer,
        int debugMode,
        int width) : IComputeShader
    {

        private float3 IntToColor(uint id)
        {
            // Mix the bits using prime numbers
            uint hash = id;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;

            // Convert to float in [0,1] range
            float r = (hash & 0xFF) / 255.0f;
            float g = ((hash >> 8) & 0xFF) / 255.0f;
            float b = ((hash >> 16) & 0xFF) / 255.0f;

            // Ensure minimum brightness and saturation
            return Hlsl.Max(new float3(r, g, b), 0.2f);
        }

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
            else if (debugMode == 3)
            {
                float3 objColor = IntToColor((uint)objectIdBuffer[pixel.X + pixel.Y * width]);
                renderTex[pixel] = new float4(objColor, 1); // Output visual world normal buffer
            }
            else renderTex[pixel] = new float4(0, 0, 0, 1); // Default path --> clear output
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
