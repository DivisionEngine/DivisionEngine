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
    /// Allows SDF objects to receive refractions.
    /// </summary>
    public class Refractions : IComponent
    {
        public bool hasRefractions = true;
        [Color(ShowAlpha = true)] public float4 absorptionColor = new float4(1f, 1f, 1f, 0.1f);
        public int maxRaySteps = 196;
        [Range(1, 16)] public int maxRecursionTraces = 4;

        public IComponent Clone() => new Refractions
        {
            maxRaySteps = maxRaySteps,
            absorptionColor = absorptionColor,
            hasRefractions = hasRefractions,
            maxRecursionTraces = maxRecursionTraces,
        };
    }
}
