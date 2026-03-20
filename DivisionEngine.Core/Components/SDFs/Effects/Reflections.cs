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
        public Reflections()
        {
            hasReflections = true;
            reflectionShadows = true;
            rayStepsFalloff = 3f;
            maxBounces = 2;
        }

        public bool hasReflections;
        public bool reflectionShadows;
        [Range(1f, 10f)] public float rayStepsFalloff;
        [Range(1, 16)] public int maxBounces;

        public IComponent Clone() => new Reflections
        {
            hasReflections = hasReflections,
            rayStepsFalloff = rayStepsFalloff,
            maxBounces = maxBounces,
        };
    }
}
