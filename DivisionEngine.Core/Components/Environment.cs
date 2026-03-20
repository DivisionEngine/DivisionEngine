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
        /// Environment with basic blue sky.
        /// </summary>
        public Environment()
        {
            backgroundColor = ColorPalette.SkyBlue;
            hdriMap = default;
        }

        [Color(true)] public float4 backgroundColor;
        public AssetRef<AudioAsset> hdriMap; // Testing this for now

        public IComponent Clone() => new Environment
        {
            backgroundColor = backgroundColor,
            hdriMap = hdriMap,
        };
    }
}
