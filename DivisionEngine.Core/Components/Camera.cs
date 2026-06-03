//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components.FieldAttributes;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents a camera in the world.
    /// </summary>
    public class Camera : IComponent
    {
        /// <summary>
        /// Whether this camera is currently active for rendering.
        /// </summary>
        [Tooltip("Whether this camera is currently active for rendering")]
        public bool isActive = true;

        // Camera vars
        [Tooltip("The viewing angle of the camera")]
        public float fieldOfView = 75f;
        [Tooltip("Near clip plane that the camera starts rendering at")]
        public float nearClip = 0.01f;
        [Tooltip("Far clip plane that the camera stops rendering at")]
        public float farClip = 10000f;

        // Depth of field vars
        public bool enableDepthOfField = false;
        [Tooltip("Attempts to calculate focal length and focus distance based on scene content")]
        public bool enableAutofocus = false;
        [Tooltip("Distance at which DOF = 0")]
        public float focusDistance = 24f;
        [Tooltip("Falloff distance for DOF")]
        public float focalLength = 26f;

        // SDF rendering vars
        [Tooltip("Max number of trace steps")]
        public int maxRaySteps = 256;
        [Tooltip("Max trace steps for shadows")]
        public int maxShadowRaySteps = 32;

        // Denoise vars
        public bool enableDivisionDenoise = true;
        [Tooltip("Enable A-Trous wavelet denoising")]
        public bool enableATrousDenoise = true;
        [Range(0f, 1f)]
        public float divisionDenoiseThreshold = 0.24f;
        [Range(1, 4)][Tooltip("Size of radius in render image to take blur samples from")]
        public int divisionDenoiseDomain = 2;
        [Range(1, 5)]
        public int aTrousStepCount = 2;

        public IComponent Clone() => new Camera
        {
            isActive = isActive,

            fieldOfView = fieldOfView,
            nearClip = nearClip,
            farClip = farClip,

            enableDepthOfField = enableDepthOfField,
            enableAutofocus = enableAutofocus,
            focusDistance = focusDistance,
            focalLength = focalLength,

            maxRaySteps = maxRaySteps,
            maxShadowRaySteps = maxShadowRaySteps,

            enableDivisionDenoise = enableDivisionDenoise,
            enableATrousDenoise = enableATrousDenoise,
            divisionDenoiseThreshold = divisionDenoiseThreshold,
            divisionDenoiseDomain = divisionDenoiseDomain,
            aTrousStepCount = aTrousStepCount,
        };
    }
}
