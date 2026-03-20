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
    /// Represents a cylinder SDF.
    /// </summary>
    public class SDFCylinder : IComponent
    {
        /// <summary>
        /// Cylinder with radius 1.0 and height 3.0.
        /// </summary>
        public SDFCylinder()
        {
            height = 3f;
            radius = 1f;
        }

        public float height;
        public float radius;

        public IComponent Clone() => new SDFCylinder
        {
            height = height,
            radius = radius,
        };
    }
}
