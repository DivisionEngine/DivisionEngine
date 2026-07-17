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
        public enum FXAADebugMode
        {
            Disabled = 0, Threshold = 1, Direction = 2,
        }

        /// <summary>
        /// Whether this camera is currently active for rendering.
        /// </summary>
        [Tooltip("Whether this camera is currently active for rendering")]
        public bool isActive = true;

        [Header("Camera Setup")]
        /// <summary>
        /// The viewing angle of the camera.
        /// </summary>
        [Tooltip("The viewing angle of the camera")]
        public float fieldOfView = 75f;
        /// <summary>
        /// Near clip plane that the camera starts rendering at.
        /// </summary>
        [Tooltip("Near clip plane that the camera starts rendering at")]
        public float nearClip = 0.01f;
        /// <summary>
        /// Far clip plane that the camera stops rendering at.
        /// </summary>
        [Tooltip("Far clip plane that the camera stops rendering at")]
        public float farClip = 10000f;

        [Header("Depth Of Field")]
        /// <summary>
        /// Enable or disable depth of field on this camera.
        /// </summary>
        public bool enableDepthOfField = false;
        /// <summary>
        /// Attempts to calculate focal length and focus distance based on scene content.
        /// </summary>
        [Tooltip("Attempts to calculate focal length and focus distance based on scene content")]
        public bool enableAutofocus = false;
        /// <summary>
        /// Distance at which DOF = 0.
        /// </summary>
        [Tooltip("Distance at which DOF = 0")]
        public float focusDistance = 24f;
        /// <summary>
        /// Falloff distance for DOF.
        /// </summary>
        [Tooltip("Falloff distance for DOF")]
        public float focalLength = 26f;

        [Header("SDF Rendering")]
        /// <summary>
        /// Max number of trace steps.
        /// </summary>
        [Tooltip("Max number of trace steps")]
        public int maxRaySteps = 256;
        /// <summary>
        /// Max trace steps for shadows.
        /// </summary>
        [Tooltip("Max trace steps for shadows")]
        public int maxShadowRaySteps = 32;

        [Header("Denoising")]
        /// <summary>
        /// Enable built-in reflections denoiser.
        /// </summary>
        public bool enableDivisionDenoise = true;
        /// <summary>
        /// Enable A-Trous wavelet denoising.
        /// </summary>
        [Tooltip("Enable A-Trous wavelet denoising")]
        public bool enableATrousDenoise = true;
        /// <summary>
        /// 0 - 1 denoising threshold for built in method.
        /// </summary>
        [Range(0f, 1f)]
        public float divisionDenoiseThreshold = 0.24f;
        /// <summary>
        /// Size of radius in render image to take blur samples from.
        /// </summary>
        [Range(1, 4)][Tooltip("Size of radius in render image to take blur samples from")]
        public int divisionDenoiseDomain = 2;
        /// <summary>
        /// Step count for a-trous denoiser.
        /// </summary>
        [Range(1, 5)]
        public int aTrousStepCount = 2;
        
        [Header("Anti-Aliasing")]
        /// <summary>
        /// Enable Fast Approximate Anti-Aliasing.
        /// </summary>
        [Tooltip("Fast Approximate Anti-Aliasing")]
        public bool enableFxaa = true;
        /// <summary>
        /// FXAA edge detection luminance threshold.
        /// </summary>
        [Tooltip("FXAA edge detection luminance threshold")]
        public float fxaaThreshold = 0.07f;
        /// <summary>
        /// Strength of the blur in the FXAA filter.
        /// </summary>
        public float fxaaStrength = 0.5f;
        [Tooltip("The size of the area edge blur")]
        public int fxaaKernelSize = 2;
        /// <summary>
        /// Debug mode for FXAA (0 = off, 1 = threshold, 2 = direction).
        /// </summary>
        [Space(6f)]
        public FXAADebugMode debugFxaa = FXAADebugMode.Disabled;

        [Header("Tonemapping")]
        public bool enableAcesTonemapper = true;

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

            enableFxaa = enableFxaa,
            fxaaThreshold = fxaaThreshold,
            fxaaStrength = fxaaStrength,
            fxaaKernelSize = fxaaKernelSize,
            debugFxaa = debugFxaa,

            enableAcesTonemapper = enableAcesTonemapper,
        };
    }
}
