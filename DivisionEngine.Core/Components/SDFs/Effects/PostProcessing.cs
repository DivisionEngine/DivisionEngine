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
    /// Component for changing the post-processing effects on a camera.
    /// </summary>
    public class PostProcessing : IComponent
    {
        [Header("Color Grading")]
        [Range(-180f, 180f)] public float hueShift = 0f;
        [Range(0f, 2f)] public float saturation = 1f;
        [Range(0f, 2f)] public float lightness = 1f;
        [Range(0f, 1f)] public float contrast = 1f;

        /// <summary>
        /// Enables or disables the vignette effect.
        /// </summary>
        [Header("Vignette")]
        [Tooltip("Enables or disables the vignette effect")]
        public bool enableVignette = false;
        /// <summary>
        /// Intensity of the vignette effect.
        /// </summary>
        [Tooltip("Intensity of the vignette effect")]
        [Range(0f, 1f)] public float vignetteIntensity = 0.5f;
        /// <summary>
        /// How far the vignette starts from the center of the screen.
        /// </summary>
        [Tooltip("How far the vignette starts from the center of the screen")]
        [Range(0f, 2f)] public float vignetteRadius = 1f;
        /// <summary>
        /// How quick the vignette falls off.
        /// </summary>
        [Tooltip("How quick the vignette falls off")]
        [Range(0f, 1f)] public float vignetteSmoothness = 0.7f;
        /// <summary>
        /// How round vs square the vignette is.
        /// </summary>
        [Tooltip("How round vs square the vignette is")]
        [Range(0f, 1f)] public float vignetteRoundness = 0.8f;
        /// <summary>
        /// Color of the vignette.
        /// </summary>
        [Tooltip("Color of the vignette")]
        [Color] public float3 vignetteColor = new float3(0f, 0f, 0f);

        /// <summary>
        /// Turns general blur on or off.
        /// </summary>
        [Header("Blur")]
        public bool enableBlur = false;
        /// <summary>
        /// Radius of the blur effect.
        /// </summary>
        [Tooltip("Radius of the blur effect")]
        [Range(0f, 50f)] public float blurRadius = 5f;

        /// <summary>
        /// Enable or disable depth of field.
        /// </summary>
        [Header("Depth of Field")]
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

        /// <summary>
        /// Turns the ACES tonemapper on or off.
        /// </summary>
        [Header("Tonemapping")]
        [Tooltip("Turns the ACES tonemapper on or off for this camera")]
        public bool enableAcesTonemapper = true;

        public IComponent Clone() => new PostProcessing
        {
            hueShift = hueShift,
            saturation = saturation,
            lightness = lightness,
            contrast = contrast,

            enableVignette = enableVignette,
            vignetteIntensity = vignetteIntensity,
            vignetteSmoothness = vignetteSmoothness,
            vignetteRoundness = vignetteRoundness,
            vignetteRadius = vignetteRadius,
            vignetteColor = vignetteColor,

            enableBlur = enableBlur,
            blurRadius = blurRadius,

            enableDepthOfField = enableDepthOfField,
            enableAutofocus = enableAutofocus,
            focusDistance = focusDistance,
            focalLength = focalLength,

            enableAcesTonemapper = enableAcesTonemapper,
        };
    }
}
