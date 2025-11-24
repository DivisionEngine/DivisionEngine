using DivisionEngine.Math;

namespace DivisionEngine.Components
{
    public struct Camera : IComponent
    {
        public static Camera Default => new Camera
        {
            fov = 70f,
            nearClip = 0.1f,
            farClip = 10000f,
            aspectRatio = 16f / 9f
        };

        public float fov;
        public float nearClip;
        public float farClip;
        public float aspectRatio;

        public static float FovToScreenDistance(float fov, float height)
        {
            return height / 2 / MathF.Tan(fov * MathF.PI / 360);
        }

        // Complete setting these methods up later

        /*public float4x4 GetCameraToWorldMatrix()
        {
            float4x4 translationMatrix = float4x4.CreateTranslation(position);
            float4x4 rotationMatrix = float4x4.CreateFromQuaternion(rotation);
            return rotationMatrix * translationMatrix;
        }*/

        public float4x4 GetCameraInverseProjectionMatrix(float aspectRatio)
        {
            float fovRad = fov * (MathF.PI / 180f);
            float f = 1.0f / MathF.Tan(fovRad / 2.0f);
            float near = 0.1f;
            float far = 1000f;
            float4x4 projectionMatrix = new float4x4
            {
                M11 = f / aspectRatio,
                M22 = f,
                M33 = (far + near) / (near - far),
                M34 = (2 * far * near) / (near - far),
                M43 = -1,
                M44 = 0
            };
            return projectionMatrix.Inverse();
        }
    }
}
