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
    /// Represents a pyramid SDF.
    /// </summary>
    public class SDFPyramid : IComponent
    {
        /// <summary>
        /// Pyramid with height of 2.0.
        /// </summary>
        public SDFPyramid()
        {
            height = 2f;
        }

        public float height;

        public IComponent Clone() => new SDFPyramid
        {
            height = height,
        };
    }
}
