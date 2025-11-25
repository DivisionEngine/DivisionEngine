using System.Runtime.InteropServices;

namespace DivisionEngine.Rendering
{
    /// <summary>
    /// Transfers SDF world data to GPU for rendering.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)] // Test if this is needed later
    public struct SDFWorldDTO
    {
        public float4x4 cameraToWorld; // 64b
        public float4x4 cameraInverseProj; // 64b
    }

    /// <summary>
    /// Transfers SDF primitive object data to GPU for rendering.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)] // Test if this is needed later
    public struct SDFPrimitiveObjectDTO
    {
        public int type; // 4b
        public float4 color; // 16b
        public float3 position; // 12b
        public float4 rotation; // 16b
        public float3 scaling; // 12b
        public float4 parameters; // 16b
    }
}
