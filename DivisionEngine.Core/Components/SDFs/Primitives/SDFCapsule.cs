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
    /// Represents a capsule SDF.
    /// </summary>
    public class SDFCapsule : IComponent
    {
        /// <summary>
        /// Capsule with radius of 1.0 and height of 3.0.
        /// </summary>
        public SDFCapsule()
        {
            height = 3f;
            radius = 1f;
        }

        public float height;
        public float radius;

        public IComponent Clone() => new SDFCapsule
        {
            height = height,
            radius = radius,
        };
    }
}
