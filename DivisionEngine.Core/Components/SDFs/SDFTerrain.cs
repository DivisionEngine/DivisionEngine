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
        /// <summary>
        /// Creates a new base terrain SDF.
        /// </summary>
        public SDFTerrain()
        {
            scale = 1000f;
            height = 100f;
            persistence = 0.5f;
            lacunarity = 2f;
        }

        public float scale;
        public float height;
        public float persistence;
        public float lacunarity;

        public IComponent Clone() => new SDFTerrain
        {
            scale = scale,
            height = height,
            persistence = persistence,
            lacunarity = lacunarity,
        };
    }
}
