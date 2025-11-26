using DivisionEngine.MathLib;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Camera component
    /// </summary>
    public class Camera : IComponent
    {
        public Camera()
        {
            fov = 70f;
            nearClip = 0.1f;
            farClip = 10000f;

            viewMatrix = Matrix.Identity4x4;
            projectionMatrix = Matrix.Identity4x4;
            inverseViewMatrix = Matrix.Identity4x4;
            inverseProjectionMatrix = Matrix.Identity4x4;
        }

        public float fov;
        public float nearClip;
        public float farClip;

        public float4x4 viewMatrix;
        public float4x4 projectionMatrix;
        public float4x4 inverseViewMatrix;
        public float4x4 inverseProjectionMatrix;
    }
}
