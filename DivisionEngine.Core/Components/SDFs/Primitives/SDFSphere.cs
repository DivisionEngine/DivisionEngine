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
    /// Represents a SDF sphere.
    /// </summary>
    public class SDFSphere : IComponent
    {
        /// <summary>
        /// Sphere with a radius of 1.0.
        /// </summary>
        public SDFSphere()
        {
            radius = 1f;
        }

        public float radius;

        public IComponent Clone() => new SDFSphere
        {
            radius = radius,
        };
    }
}
