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
            if (WorldManager.CurrentWorld == null) return (new SDFWorldDTO(), []);

            World w = WorldManager.CurrentWorld;

            SDFWorldDTO worldData = new SDFWorldDTO();
            List<SDFPrimitiveObjectDTO> sdfPrimitives = [];

            // Gather camera world data
            foreach (var (entity, components) in w.QueryData(typeof(Transform), typeof(Camera)))
            {
                Transform transform = (Transform)components[0];
                Camera camera = (Camera)components[1];

                worldData.cameraOrigin = transform.position;
                worldData.cameraToWorld = camera.inverseViewMatrix;
                worldData.cameraInverseProj = camera.inverseProjectionMatrix;
                break; // Use first camera
            }

            // Gather sphere primitives
            foreach (var (entity, components) in w.QueryData(typeof(Transform), typeof(SDFSphere)))
            {
                Transform transform = (Transform)components[0];
                SDFSphere sphere = (SDFSphere)components[1];

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
