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
        /// <summary>
        /// Environment with basic blue sky and shadow scale = 10 meters.
        /// </summary>
        public Environment()
        {
            backgroundColor = ColorPalette.SkyBlue;
            ambientStrength = 0.15f;
            shadowScale = 20f;
            hdriMap = default;
        }

        [Color(true)] public float4 backgroundColor;
        [Range(0f, 1f)] public float ambientStrength;
        [Tooltip("Shadow penumbra range in meters")] public float shadowScale;
        public AssetRef<AudioAsset> hdriMap; // Testing this for now

        public IComponent Clone() => new Environment
        {
            backgroundColor = backgroundColor,
            ambientStrength = ambientStrength,
            shadowScale = shadowScale,
            hdriMap = hdriMap,
        };
    }
}
