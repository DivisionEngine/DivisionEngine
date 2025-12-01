using DivisionEngine.MathLib;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Camera component
    /// </summary>
    public class Camera : IComponent
    {
        /// <summary>
        /// Creates a camera with Fov = 70, max ray steps = 256.
        /// </summary>
        public Camera()
        {
            fov = 70f;
            nearClip = 0.1f;
            farClip = 10000f;

            viewMatrix = Matrix.Identity4x4;
            projectionMatrix = Matrix.Identity4x4;
            cameraToWorld = Matrix.Identity4x4;
            inverseProjectionMatrix = Matrix.Identity4x4;

            maxRaySteps = 256;
            maxShadowRaySteps = 128;
        }

        // Camera vars
        public float fov;
        public float nearClip;
        public float farClip;

        public float4x4 viewMatrix;
        public float4x4 projectionMatrix;
        public float4x4 cameraToWorld; // Inverse view matrix
        public float4x4 inverseProjectionMatrix;

        // SDF rendering vars
        public int maxRaySteps;
        public int maxShadowRaySteps;
    }
}
