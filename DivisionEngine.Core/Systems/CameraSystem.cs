using DivisionEngine.Components;
using DivisionEngine.MathLib;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Systems
{
    public class CameraSystem() : SystemBase
    {
        public override void Update()
        {
            if (WorldManager.CurrentWorld == null) return;
            World w = WorldManager.CurrentWorld;

            foreach (var (entity, components) in w.QueryData(typeof(Transform), typeof(Camera)))
            {
                Transform transform = (Transform)components[0];
                Camera cam = (Camera)components[1];
                UpdateCameraMatrices(transform, cam);
            }
        }

        private void UpdateCameraMatrices(Transform transform, Camera camera)
        {
            camera.viewMatrix = CalcCameraViewMatrix(transform);
            camera.projectionMatrix = CalcCameraProjectionMatrix(camera);
            Debug.Info("Update camera view matrix: " + camera.viewMatrix.Float4x4ToMatrix4x4().ToString());
            Debug.Info("Update camera projec matrix: " + camera.projectionMatrix.Float4x4ToMatrix4x4().ToString());

            camera.inverseViewMatrix = Matrix.Inverse(camera.viewMatrix);
            camera.inverseProjectionMatrix = Matrix.Inverse(camera.projectionMatrix);
        }

        private float4x4 CalcCameraViewMatrix(Transform transform)
        {
            return Matrix.Inverse(
                Matrix.Multiply(
                    Matrix.CreateMatrix4x4FromQuaternion(transform.rotation),
                    Matrix.CreateMatrix4x4FromTranslation(transform.position)));
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
