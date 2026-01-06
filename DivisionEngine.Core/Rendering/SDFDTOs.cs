using ComputeSharp;

namespace DivisionEngine.Rendering
{
    /// <summary>
    /// Transfers SDF world data to GPU for rendering.
    /// </summary>
    public struct SDFWorldDTO
    {
        // Camera Properties
        public float3 cameraOrigin; // 12b
        public float4x4 cameraToWorld; // 64b
        public float4x4 cameraInverseProj; // 64b
        public float nearPlane; // 4b
        public float farPlane; // 4b

        // Environment Properties
        public float4 backgroundColor; // 16b
        public int maxRaySteps; // 4b
        public int maxShadowRaySteps; // 4b
        public int maxRayBounces; // 4b - used for reflections and refractions
    }

    /// <summary>
    /// Transfers SDF primitive object data to GPU for rendering.
    /// </summary>
    public struct SDFPrimitiveObjectDTO
    {
        // Base Properties
        public int type; // 4b
        public float3 position; // 12b
        public float4 rotation; // 16b
        public float3 scaling; // 12b
        public float4 parameters; // 16b

        // Material Properties
        public float4 color; // 16b
        public float metallic; // 4b
        public float roughness; // 4b

        // Effects
        public float2 shadowDistances; // 8b
        public bool2 shadowEffects; // 8b
        public Bool hasReflection; // 4b - must use ComputeSharp.Bool type when in DTO
        public Bool hasRefraction; // 4b
        public int reflectionMaxSteps; // 4b
        public int refractionMaxSteps; // 4b
        public float ior; // 4b
    }

    /// <summary>
    /// Transfers SDF light data to GPU for rendering.
    /// </summary>
    public struct SDFLightDTO
    {
        // Base Properties
        public int type; // 4b
        public float3 position; // 12b
        public float4 rotation; // 16b

        // Light Properties
        public float4 color; // 16b
        public float intensity; // 4b
    }
}
