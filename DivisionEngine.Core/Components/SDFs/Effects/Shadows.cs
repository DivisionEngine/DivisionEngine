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
    /// Allows SDF objects to cast and receive penumbra shadows.
    /// </summary>
    public class Shadows : IComponent
    {
        public enum ShadowType
        {
            Hard = 0, Soft = 1,
        }

        [Header("Setup")]
        public ShadowType shadowStyle = ShadowType.Soft;
        public bool shadowCaster = true;
        public bool shadowReceiver = true;

        [Header("Distances")]
        public float penumbraDistance = 40f;
        public float minDistance = 0.001f;
        public float maxDistance = 100f;

        public IComponent Clone() => new Shadows
        {
            shadowStyle = shadowStyle,
            shadowCaster = shadowCaster,
            shadowReceiver = shadowReceiver,

            penumbraDistance = penumbraDistance,
            minDistance = minDistance,
            maxDistance = maxDistance,
        };
    }
}
