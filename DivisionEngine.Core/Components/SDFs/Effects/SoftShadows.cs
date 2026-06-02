//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to cast and receive shadows.
    /// </summary>
    public class SoftShadows : IComponent
    {
        public bool shadowCaster = true;
        public bool shadowReceiver = true;

        public float minDistance = 0.001f;
        public float maxDistance = 100f;

        public IComponent Clone() => new SoftShadows
        {
            shadowCaster = shadowCaster,
            shadowReceiver = shadowReceiver,

            minDistance = minDistance,
            maxDistance = maxDistance,
        };
    }
}
