using DivisionEngine.Components;
using DivisionEngine.Rendering;

namespace DivisionEngine.Systems
{
    public class SDFRenderSystem : SystemBase
    {
        public static SDFWorldDTO PreparedWorldDTO { get; private set; }
        public static SDFPrimitiveObjectDTO[] PreparedPrimitivesDTO { get; private set; } = [];

        public override void Render()
        {
            (PreparedWorldDTO, PreparedPrimitivesDTO) = GetFullWorldSDFData();
        }

        public static (SDFWorldDTO, SDFPrimitiveObjectDTO[]) GetFullWorldSDFData()
        {
            SDFWorldDTO worldData = new SDFWorldDTO();
            List<SDFPrimitiveObjectDTO> sdfPrimitives = [];

            // Gather camera world data
            foreach (var (_, transform, camera) in W.QueryData<Transform, Camera>())
            {
                worldData.cameraOrigin = transform.position;
                worldData.cameraToWorld = camera.inverseViewMatrix;
                worldData.cameraInverseProj = camera.inverseProjectionMatrix;
                break; // Use first camera
            }

            // Gather sphere primitives
            foreach (var (_, transform, sphere) in W.QueryData<Transform, SDFSphere>())
            {
                sdfPrimitives.Add(new SDFPrimitiveObjectDTO
                {
                    type = 0, // Sphere type
                    color = sphere.color,
                    position = transform.position,
                    rotation = transform.rotation,
                    scaling = transform.scaling,
                    parameters = new float4(sphere.radius, 0, 0, 0)
                });
            }

            // Space to add more SDF primitives in the future

            return (worldData, sdfPrimitives.ToArray());
        }
    }
}
