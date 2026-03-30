//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components.FieldAttributes;

namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Component for volumetric fog effects.
    /// </summary>
    public class VolumetricFog : IComponent
    {
        /// <summary>
        /// Default fog volume.
        /// </summary>
        public VolumetricFog()
        {
            density = 0.05f;
            color = new float4(0.5f, 0.6f, 0.7f, 1.0f);
            absorption = 0.1f;
            scattering = 0.5f;
            anisotropy = 0.6f;
        }

        /// <summary>
        /// Density of the fog (higher = thicker fog).
        /// </summary>
        public float density;

        /// <summary>
        /// Color of the fog.
        /// </summary>
        public float4 color;

        /// <summary>
        /// Light absorption factor (how much light is absorbed by fog).
        /// </summary>
        public float absorption;

        /// <summary>
        /// Light scattering factor (how much light is scattered).
        /// </summary>
        public float scattering;

        /// <summary>
        /// Anisotropy for Henyey-Greenstein phase function (-1 = back scattering, 0 = isotropic, 1 = forward scattering).
        /// </summary>
        [Range(0f, 1f)] public float anisotropy;

        public IComponent Clone() => new VolumetricFog
        {
            density = density,
            color = color,
            absorption = absorption,
            scattering = scattering,
            anisotropy = anisotropy,
        };
    }
}
