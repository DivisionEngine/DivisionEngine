//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Rendering
{
    /// <summary>
    /// Transfers SDF world data to GPU for rendering.
    /// </summary>
    public struct SDFWorldDTO
    {
        // Camera Properties
        public float3 cameraOrigin; // 12b
        public float3 camForward; // 12b
        public float3 camRight; // 12b
        public float3 camUp; // 12b
        public float nearPlane; // 4b
        public float farPlane; // 4b
        public float camScreenDist; // 4b

        // Environment Properties
        public float4 skyColor; // 16b
        public float4 bottomSkyColor; // 16b
        public float4 middleSkyColor; // 16b
        public float4 topSkyColor; // 16b
        public int hdriTexMetaID; // 4b
        public int skyType; // 4b
        public float skyIntensity; // 4b
        public float ambientStrength; // 4b
        public int maxRaySteps; // 4b
        public int maxShadowRaySteps; // 4b
        public float shadowScale; // 4b
        public float3 mainLightDir; // 12b - precalculate main light direction

        // DoF Properties
        public float focusDistance; // 4b
        public float focalLength; // 4b

        // Denoise Properties
        public int enableDivisionDenoise; // 4b
        public int enableATrousDenoise; // 4b
        public float divisionThreshold; // 4b
        public int divisionDomain; // 4b
        public int aTrousStepCount; // 4b

        // Fog settings
        public float fogDensity; // 4b
        public float4 fogColor; // 16b
        public float fogAbsorption; // 4b
        public float fogScattering; // 4b
        public float fogAnisotropy; // 4b
    }

    /// <summary>
    /// Transfers SDF object data to GPU for rendering.
    /// </summary>
    public struct SDFObjectDTO
    {
        // Base Properties
        public uint entityId; // 4b
        public int type; // 4b
        public float3 position; // 12b
        public float4 rotation; // 16b
        public float3 scaling; // 12b
        public float4 parameters; // 16b
        public float4 parameters2; // 16b
        public float4 parameters3; // 16b
        public float4 parameters4; // 16b
        public float4 parameters5; // 16b
        public float4 parameters6; // 16b
        public float stepBias; // 4b

        // Material Properties
        public int albedoTexMetaID; // 4b
        public int normalTexMetaID; // 4b
        public float normalStrength; // 4b
        public int displaceTexMetaID; // 4b
        public float displaceStrength; // 4b
        public int roughTexMetaID; // 4b
        public int metalTexMetaID; // 4b
        public int emissionTexMetaID; // 4b
        public float4 texTilingOffset; // 16b - xy is tiling, zw is offsets
        public float4 color; // 16b
        public float metallic; // 4b
        public float roughness; // 4b
        public float specular; // 4b
        public float ior; // 4b
        public float3 aoValues; // 12b - stores AO, AO Range, and AO Falloff respectively
        public float reflectance; // 4b

        // Precalculated Material Properties
        public float3 f0_reflectance; // 12b
        public float f0_dielectric; // 4b

        // Effect Properties
        public float2 shadowDistances; // 8b
        public bool2 shadowEffects; // 8b
        public float4 absorptionColor; // 16b
        public int hasReflection; // 4b - must use integer type when in DTO
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
        public float4 color; // 16b
        public float intensity; // 4b

        // Point Light Properties
        public float radius; // 4b
    }
}
