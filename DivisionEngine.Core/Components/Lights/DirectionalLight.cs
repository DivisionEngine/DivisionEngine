//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.Lights
{
    /// <summary>
    /// Represents a directional light in the world.
    /// </summary>
    public class DirectionalLight : IComponent
    {
        /// <summary>
        /// White directional light with base direction and default intensity.
        /// </summary>
        public DirectionalLight()
        {
            color = ColorPalette.White;
            intensity = 1f;
        }

        public float4 color;
        public float intensity;

        public IComponent Clone() => new DirectionalLight
        {
            color = color,
            intensity = intensity,
        };
    }
}
