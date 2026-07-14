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

namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents the world environment.
    /// </summary>
    public class Environment : IComponent
    {
        public enum SkyType
        {
            Solid = 0, Gradient = 1, HDRI = 2
        }

        [Header("Lighting")]
        [Range(0f, 1f)] public float ambientStrength = 0.15f;
        [Tooltip("Shadow penumbra range in meters")] public float shadowScale = 20f;
        public SkyType skyType = SkyType.Solid;
        [Min(0f)] public float skyIntensity = 1f;

        [Header("Single Color Sky")]
        [Color(false)] public float4 skyColor = ColorPalette.SkyBlue;

        [Header("Gradient Sky")]
        [Color(false)] public float4 topSkyColor = ColorPalette.DeepSkyBlue;
        [Color(false)] public float4 middleSkyColor = ColorPalette.SkyBlue;
        [Color(false)] public float4 bottomSkyColor = ColorPalette.SandyBrown;

        [Header("HDRI Sky")]
        public AssetRef<TextureAsset> hdriMap = default;

        public IComponent Clone() => new Environment
        {
            ambientStrength = ambientStrength,
            shadowScale = shadowScale,
            skyType = skyType,
            skyIntensity = skyIntensity,

            skyColor = skyColor,
            bottomSkyColor = bottomSkyColor,
            middleSkyColor = middleSkyColor,
            topSkyColor = topSkyColor,
            hdriMap = hdriMap,
        };
    }
}
