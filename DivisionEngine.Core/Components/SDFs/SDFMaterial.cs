//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;
using DivisionEngine.Projects.Assets;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents material properties of an entity.
    /// </summary>
    public class SDFMaterial : IComponent
    {
        // Air/Vacuum
        const float IOR_AIR = 1.0f;

        // Liquids
        const float IOR_WATER = 1.333f;
        const float IOR_ICE = 1.31f;
        const float IOR_ALCOHOL = 1.36f;

        // Glass Types
        const float IOR_GLASS = 1.5f;
        const float IOR_CROWN_GLASS = 1.52f;
        const float IOR_FLINT_GLASS = 1.6f;

        // Crystals & Gems
        const float IOR_QUARTZ = 1.54f;
        const float IOR_EMERALD = 1.57f;
        const float IOR_RUBY = 1.77f;
        const float IOR_DIAMOND = 2.417f;

        // Plastics
        const float IOR_ACRYLIC = 1.49f;
        const float IOR_POLYCARBONATE = 1.58f;

        // Textures
        public AssetRef<TextureAsset> albedoMap = default;
        public AssetRef<TextureAsset> normalMap = default;
        public AssetRef<TextureAsset> roughnessMap = default;
        public AssetRef<TextureAsset> metallicMap = default;
        public AssetRef<TextureAsset> emissiveMap = default;

        // UV scale/offset
        public float2 uvScale = new float2(1f, 1f);
        public float2 uvOffset = float2.Zero;

        [Color(false)] public float4 albedoColor = ColorPalette.White;
        [Range(0f, 1f)] public float metallic = 0.8f;
        [Range(0f, 1f)] public float roughness = 0.2f;
        public float specular = 1f;

        [Tooltip("Index of refraction")]
        [Range(1f, 3f)] public float ior = 1.0f;

        [Range(0f, 1f)] public float ambientOcclusion = 0.7f;

        [Tooltip("Ambient occlusion maximum falloff distance")]
        [Range(0f, 10f)] public float ambientRange = 1f;

        [Tooltip("Ambient occlusion falloff curve, higher value = faster falloff")]
        [Range(0f, 10f)] public float ambientFalloff = 2f;

        [Tooltip("Raymarched reflection intensity, useful for combining with refractions")]
        [Range(0f, 10f)] public float reflectance = 2f;

        [Tooltip("A multiplier given to any SDF that can modify the step size to it")]
        [Range(0.1f, 1.5f)] public float stepBias = 1f;

        public IComponent Clone() => new SDFMaterial
        {
            albedoMap = albedoMap,
            normalMap = normalMap,
            roughnessMap = roughnessMap,
            metallicMap = metallicMap,
            emissiveMap = emissiveMap,
            uvScale = uvScale,
            uvOffset = uvOffset,

            albedoColor = albedoColor,
            metallic = metallic,
            roughness = roughness,
            specular = specular,
            ior = ior,
            ambientOcclusion = ambientOcclusion,
            ambientRange = ambientRange,
            ambientFalloff = ambientFalloff,
            reflectance = reflectance,
            stepBias = stepBias,
        };
    }
}
