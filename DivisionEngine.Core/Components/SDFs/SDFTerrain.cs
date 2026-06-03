//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Used for building SDF terrains.
    /// </summary>
    public class SDFTerrain : IComponent
    {
        public float scale = 2000f;
        public float height = 100f;
        public float baseGain = 0.5f;
        public float lacunarity = 2f;
        public int octaves = 2;

        public float erosionStrength = 0.22f;
        public float gullyWeight = 0.5f;
        public float erosionDetail = 1.5f;
        public float erosionScale = 0.15f;
        public int erosionOctaves = 5;
        public float erosionLacunarity = 2.0f;
        public float erosionGain = 0.5f;
        public float cellScale = 0.7f;
        public float normalization = 0.5f;

        public float4 rounding = new float4(1f, 1f, 1f, 1f);

        public IComponent Clone() => new SDFTerrain
        {
            scale = scale,
            height = height,
            baseGain = baseGain,
            lacunarity = lacunarity,
            octaves = octaves,

            erosionStrength = erosionStrength,
            gullyWeight = gullyWeight,
            erosionDetail = erosionDetail,
            erosionScale = erosionScale,
            erosionOctaves = erosionOctaves,
            erosionLacunarity = erosionLacunarity,
            erosionGain = erosionGain,
            cellScale = cellScale,
            normalization = normalization,

            rounding = rounding,
        };
    }
}
