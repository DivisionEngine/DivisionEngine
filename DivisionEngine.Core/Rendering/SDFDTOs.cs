namespace DivisionEngine.Rendering
{
    /// <summary>
    /// Transfers SDF world data to GPU for rendering.
    /// </summary>
    public struct SDFWorldDTO
    {
        public float3 cameraOrigin; // 12b
        public float4x4 cameraToWorld; // 64b
        public float4x4 cameraInverseProj; // 64b

        public int maxRaySteps; // 4b
        public int maxShadowRaySteps; // 4b
    }

    /// <summary>
    /// Transfers SDF primitive object data to GPU for rendering.
    /// </summary>
    public struct SDFPrimitiveObjectDTO
    {
        public int type; // 4b
        public float4 color; // 16b
        public float3 position; // 12b
        public float4 rotation; // 16b
        public float3 scaling; // 12b
        public float4 parameters; // 16b

        public float2 shadowDistances; // 8b
        public bool2 shadowEffects; // 8b
    }
}
