//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components.FieldAttributes;

namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to receive reflections.
    /// </summary>
    public class Reflections : IComponent
    {
        public bool hasReflections = true;
        public bool reflectionShadows = true;
        [Range(1f, 10f)] public float rayStepsFalloff = 3f;
        [Range(1, 16)] public int maxBounces = 2;

        public IComponent Clone() => new Reflections
        {
            hasReflections = hasReflections,
            rayStepsFalloff = rayStepsFalloff,
            maxBounces = maxBounces,
        };
    }
}
