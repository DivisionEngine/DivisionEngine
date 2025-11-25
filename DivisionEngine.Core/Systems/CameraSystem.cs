using DivisionEngine.Components;
using DivisionEngine.Math;

namespace DivisionEngine.Systems
{
    public class CameraSystem() : SystemBase
    {
        public override void Update()
        {
            foreach (var item in WorldManager.CurrentWorld?.Query<Camera>())
            {

            }
        }

        private void UpdateCameraMatrices(Transform transform, Camera camera)
        {

        }

        private float4x4 CalcCameraViewMatrix()
        {
            return Matrix.Identity4x4;
            // Finish this
        }

        private float4x4 CalcCameraProjectionMatrix()
        {
            return Matrix.Identity4x4;
            // Finish this
        }
    }
}
