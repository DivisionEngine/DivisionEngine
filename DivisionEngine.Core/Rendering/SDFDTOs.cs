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
        public float4 invCamRot; // 16b
        //public float4x4 cameraToWorld; // 64b
        //public float4x4 cameraInverseProj; // 64b
        public float nearPlane; // 4b
        public float farPlane; // 4b
        public float camScreenDist; // 4b

        // Environment Properties
        public float4 backgroundColor; // 16b
        public int maxRaySteps; // 4b
        public int maxShadowRaySteps; // 4b

        // DoF Properties
        public float focusDistance; // 4b
        public float focalLength; // 4b

        // Denoise Properties
        public int enableDivisionDenoise; // 4b
        public int enableATrousDenoise; // 4b
        public float divisionThreshold; // 4b
        public int divisionDomain; // 4b
        public int aTrousStepCount; // 4b
    }

    /// <summary>
    /// Transfers SDF primitive object data to GPU for rendering.
    /// </summary>
    public struct SDFPrimitiveObjectDTO
    {
        // Base Properties
        public uint entityId; // 4b
        public int type; // 4b
        public float3 position; // 12b
        public float4 rotation; // 16b
        public float3 scaling; // 12b
        public float4 parameters; // 16b

        // Material Properties
        public float4 color; // 16b
        public float metallic; // 4b
        public float roughness; // 4b
        public float specular; // 4b
        public float ior; // 4b
        public float ao; // 4b
        public float reflectance; // 4b

        // Effect Properties
        public float2 shadowDistances; // 8b
        public bool2 shadowEffects; // 8b
        public float4 absorptionColor; // 16b
        public int hasReflection; // 4b - must use int type when in DTO
        public int hasRefraction; // 4b
        public int reflectionShadows; // 4b
        public float reflectRayStepFalloff; // 4b
        public int reflectionMaxBounces; // 4b
        public int refractionMaxSteps; // 4b
        public int refractMaxRecursion; // 4b
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
