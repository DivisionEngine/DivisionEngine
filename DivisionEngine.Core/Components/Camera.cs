using DivisionEngine.Components.FieldAttributes;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents a camera in the world.
    /// </summary>
    public class Camera : IComponent
    {
        /// <summary>
        /// Camera with Fov = 75, max ray steps = 256, focus dist = 10, and denoise = true.
        /// </summary>
        public Camera()
        {
            fieldOfView = 75f;
            nearClip = 0.01f;
            farClip = 10000f;

            enableDepthOfField = false;
            focusDistance = 24f;
            focalLength = 26f;

            maxRaySteps = 256;
            maxShadowRaySteps = 128;

            enableDivisionDenoise = true;
            enableATrousDenoise = true;
            divisionDenoiseThreshold = 0.24f;
            divisionDenoiseDomain = 2;
            aTrousStepCount = 2;
        }

        // Camera vars
        [Tooltip("The viewing angle of the camera")] public float fieldOfView;
        [Tooltip("Near clip plane that the camera starts rendering at")] public float nearClip;
        [Tooltip("Far clip plane that the camera stops rendering at")] public float farClip;

        // Depth of field vars
        public bool enableDepthOfField;
        [Tooltip("Distance at which DOF = 0")] public float focusDistance;
        [Tooltip("Falloff distance for DOF")] public float focalLength;

        // SDF rendering vars
        [Tooltip("Max number of trace steps")] public int maxRaySteps;
        [Tooltip("Max trace steps for shadows")] public int maxShadowRaySteps;

        // Denoise vars
        public bool enableDivisionDenoise;
        [Tooltip("Enable A-Trous wavelet denoising")] public bool enableATrousDenoise;
        [Range(0f, 1f)] public float divisionDenoiseThreshold;
        [Tooltip("Size of radius in render image to take blur samples from")][Range(1, 4)] public int divisionDenoiseDomain;
        [Range(1, 5)] public int aTrousStepCount;

        public IComponent Clone() => new Camera
        {
            fieldOfView = fieldOfView,
            nearClip = nearClip,
            farClip = farClip,

            enableDepthOfField = enableDepthOfField,
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
