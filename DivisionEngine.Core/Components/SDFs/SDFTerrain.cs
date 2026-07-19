//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components.FieldAttributes;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Used for building SDF terrains.
    /// </summary>
    public class SDFTerrain : IComponent
    {
        [Header("Terrain Settings")]
        [Range(1f, 1000f)] public float scale = 100f;
        [Range(0f, 500f)] public float height = 20f;
        [Range(0f, 1f)] public float baseGain = 0.5f;
        [Range(0.5f, 4f)] public float lacunarity = 2f;
        [Range(1, 8)] public int octaves = 4;

        [Header("Erosion Settings")]
        [Range(0f, 2f)] public float erosionStrength = 0f;
        [Range(0f, 1f)] public float gullyWeight = 0.5f;
        [Range(0f, 1f)] public float erosionDetail = 0.5f;
        [Range(0.1f, 10f)] public float erosionScale = 1f;
        [Range(1, 6)] public int erosionOctaves = 3;
        [Range(0.5f, 4f)] public float erosionLacunarity = 2f;
        [Range(0f, 1f)] public float erosionGain = 0.5f;
        [Range(0.1f, 2f)] public float cellScale = 0.5f;
        [Range(0f, 2f)] public float normalization = 1f;
        public float4 rounding = new float4(1f, 1f, 1f, 1f);

        [Header("Grass Settings")]
        [Range(0f, 20f)] public float grassDensity = 3f;
        [Range(0f, 5f)] public float grassHeight = 0.5f;
        [Range(0f, 0.5f)] public float grassRadius = 0.02f;
        [Range(0f, 2f)] public float grassBend = 0.3f;

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

            grassDensity = grassDensity,
            grassHeight = grassHeight,
            grassRadius = grassRadius,
            grassBend = grassBend,
        };
    }
}
