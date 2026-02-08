using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

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

            viewMatrix = Matrix.Identity4x4;
            projectionMatrix = Matrix.Identity4x4;
            cameraToWorld = Matrix.Identity4x4;
            inverseProjectionMatrix = Matrix.Identity4x4;

            focusDistance = 10f;
            apertureSize = 0.01f;
            depthOfFieldSamples = 1;

            maxRaySteps = 256;
            maxShadowRaySteps = 128;

            enableDenoise = true;
        }

        // Camera vars
        public float fieldOfView;
        public float nearClip;
        public float farClip;

        // Matrices
        public float4x4 viewMatrix;
        public float4x4 projectionMatrix;
        public float4x4 cameraToWorld; // Inverse view matrix
        public float4x4 inverseProjectionMatrix;

        // Depth of field vars
        public float focusDistance;
        public float apertureSize;
        [Range(0, 10)] public int depthOfFieldSamples;

        // SDF rendering vars
        public int maxRaySteps;
        public int maxShadowRaySteps;

        // Denoising Variables
        public bool enableDenoise;

        public IComponent Clone() => new Camera
        {
            fieldOfView = fieldOfView,
            nearClip = nearClip,
            farClip = farClip,

            viewMatrix = viewMatrix,
            projectionMatrix = projectionMatrix,
            cameraToWorld = cameraToWorld,
            inverseProjectionMatrix = inverseProjectionMatrix,

            focusDistance = focusDistance,
            apertureSize = apertureSize,
            depthOfFieldSamples = depthOfFieldSamples,

            maxRaySteps = maxRaySteps,
            maxShadowRaySteps = maxShadowRaySteps,

            enableDenoise = enableDenoise,
        };
    }
}
