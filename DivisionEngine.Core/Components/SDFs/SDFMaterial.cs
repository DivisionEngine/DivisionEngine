//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

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

        /// <summary>
        /// White material with metallic = 0, roughness = 0.5, specular = 0.5, ior = 1, and ao = 0.
        /// </summary>
        public SDFMaterial()
        {
            albedoColor = ColorPalette.White;
            metallic = 0.8f;
            roughness = 0.2f;
            specular = 1f;
            ior = 1.0f;
            ambientOcclusion = 0.3f;
            reflectance = 2f;
        }

        [Color(false)] public float4 albedoColor;
        [Range(0f, 1f)] public float metallic;
        [Range(0f, 1f)] public float roughness;
        public float specular;
        [Range(1f, 3f)] public float ior;
        [Range(0f, 1f)] public float ambientOcclusion;
        [Range(0f, 10f)] public float reflectance;

        public IComponent Clone() => new SDFMaterial
        {
            albedoColor = albedoColor,
            metallic = metallic,
            roughness = roughness,
            specular = specular,
            ior = ior,
            ambientOcclusion = ambientOcclusion,
            reflectance = reflectance,
        };
    }
}
