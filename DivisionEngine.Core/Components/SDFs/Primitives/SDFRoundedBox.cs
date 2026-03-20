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
    /// Represents a rounded box SDF.
    /// </summary>
    public class SDFRoundedBox : IComponent
    {
        /// <summary>
        /// Rounded box with a size of 1.0 and a bevel of 0.05.
        /// </summary>
        public SDFRoundedBox()
        {
            size = new float3(1f, 1f, 1f);
            bevel = 0.05f;
        }

        public float3 size;
        public float bevel;

        public IComponent Clone() => new SDFRoundedBox
        {
            size = size,
            bevel = bevel,
        };
    }
}
