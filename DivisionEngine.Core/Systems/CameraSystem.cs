using DivisionEngine.Components;
using DivisionEngine.Math;

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
            camera.projectionMatrix = CalcCameraProjectionMatrix();

            camera.inverseViewMatrix = Matrix.Inverse(camera.viewMatrix);
            camera.inverseProjectionMatrix = Matrix.Inverse(camera.projectionMatrix);
        }

        private float4x4 CalcCameraViewMatrix(Transform transform)
        {
            // calc cam forward direction
            float3 zaxis = Vector.Normalize()
        }

        private float4x4 CalcCameraProjectionMatrix()
        {
            return Matrix.Identity4x4;
            // Finish this
        }

        private float4x4 CreateLookAtMatrix()
        {
            return Matrix.Identity4x4;
            // Finish this
        }

        private float4x4 CreatePerspectiveFOVMatrix()
        {
            return Matrix.Identity4x4;
            // Finish this
        }
    }
}
