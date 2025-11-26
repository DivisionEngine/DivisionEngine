using DivisionEngine.Components;
using DivisionEngine.MathLib;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Systems
{
    public class CameraSystem : SystemBase
    {
        public override void Render()
        {
            foreach (var (_, transform, camera) in W.QueryData<Transform, Camera>())
                UpdateCameraMatrices(transform, camera);
        }

        private static void UpdateCameraMatrices(Transform transform, Camera camera)
        {
            float4x4 camToWorld = CalcCameraToWorldMatrix(transform);
            camera.cameraToWorld = camToWorld;
            camera.viewMatrix = Matrix.Inverse(camToWorld);
            camera.projectionMatrix = CalcCameraProjectionMatrix(camera);
            camera.inverseProjectionMatrix = Matrix.Inverse(camera.projectionMatrix);
        }

        private static float4x4 CalcCameraToWorldMatrix(Transform transform)
        {
            return Matrix.Multiply(
                Matrix.CreateMatrix4x4FromQuaternion(transform.rotation),
                Matrix.CreateMatrix4x4FromTranslation(transform.position));
        }

        private static float4x4 CalcCameraProjectionMatrix(Camera cam)
        {
            float fovRad = Math.Deg2Rad * cam.fov;
            float tanHalfFov = Math.Tan(fovRad / 2f);

            float m1122 = 1f / tanHalfFov; // (usually 1f / (aspect * tanHalfFov)) but aspect ratio is in shader instead
            //float m22 = 1f / tanHalfFov;
            float m33 = cam.farClip / (cam.nearClip - cam.farClip);
            float m43 = (cam.farClip * cam.nearClip) / (cam.nearClip - cam.farClip);

            return new float4x4(
                m1122, 0, 0, 0,
                0, m1122, 0, 0,
                0, 0, m33, -1,
                0, 0, m43, 0);
        }

        public static float FovToScreenDistance(float fov, float height)
        {
            return height / 2 / MathF.Tan(fov * MathF.PI / 360);
        }
    }
}
