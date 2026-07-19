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
        [Range(16f, 8192f)] public int heightmapSize = 2048;
        [Range(1f, 10000f)] public float scale = 500f;
        [Range(0f, 500f)] public float height = 12f;
        [Range(0.01f, 1f)] public float frequency = 0.01f;
        [Range(1, 16)] public int octaves = 6;
        [Range(1.1f, 4f)] public float lacunarity = 2f;
        [Range(0f, 1f)] public float persistence = 0.5f;
        public bool worldSpace = false;

        [Header("Ridge Noise")]
        [Range(0f, 1f)] public float ridgeBlend = 0f;
        [Range(0f, 2f)] public float ridgeWeight = 0.5f;

        [Header("Edge Falloff")]
        [Range(0f, 1f)] public float edgeFalloff = 0.1f;

        public IComponent Clone() => new SDFTerrain
        {
            heightmapSize = heightmapSize,
            scale = scale,
            height = height,
            frequency = frequency,
            octaves = octaves,
            lacunarity = lacunarity,
            persistence = persistence,
            worldSpace = worldSpace,

            ridgeBlend = ridgeBlend,
            ridgeWeight = ridgeWeight,
            edgeFalloff = edgeFalloff,
        };
    }
}
