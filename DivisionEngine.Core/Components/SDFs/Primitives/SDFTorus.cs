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
    /// Represents a SDF donut shape.
    /// </summary>
    public class SDFTorus : IComponent
    {
        /// <summary>
        /// Torus with whole radius of 2.0 and ring radius of 1.0.
        /// </summary>
        public SDFTorus()
        {
            wholeRadius = 2f;
            ringRadius = 1f;
        }

        public float wholeRadius;
        public float ringRadius;

        public IComponent Clone() => new SDFTorus
        {
            wholeRadius = wholeRadius,
            ringRadius = ringRadius,
        };
    }
}
