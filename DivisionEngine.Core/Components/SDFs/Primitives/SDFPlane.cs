//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Components.SDFs.Primitives
{
    /// <summary>
    /// Represents a plane SDF.
    /// </summary>
    public class SDFPlane : IComponent
    {
        /// <summary>
        /// Plane with a normal vector directly up and a height of 1.0.
        /// </summary>
        public SDFPlane()
        {
            normal = new float3(0f, 1f, 0f);
            height = 1f;
        }

        public float3 normal;
        public float height;

        public IComponent Clone() => new SDFPlane
        {
            normal = normal,
            height = height,
        };
    }
}
