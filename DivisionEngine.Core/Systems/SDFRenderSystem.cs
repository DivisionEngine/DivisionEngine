using DivisionEngine.Components;
using DivisionEngine.Components.SDFs;
using DivisionEngine.Components.SDFs.Effects;
using DivisionEngine.Rendering;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Critial system in charge of packaging world render information before each render cycle.
    /// </summary>
    public class SDFRenderSystem : SystemBase
    {
        /// <summary>
        /// Prepared settings for the world information.
        /// </summary>
        public static SDFWorldDTO PreparedWorldDTO { get; private set; }

        /// <summary>
        /// Prepared settings for all SDF primitives in the world.
        /// </summary>
        public static SDFPrimitiveObjectDTO[] PreparedPrimitivesDTO { get; private set; } = [];

        /// <summary>
        /// Called right before the world is rendered to screen.
        /// </summary>
        public override void Render()
        {
            (PreparedWorldDTO, PreparedPrimitivesDTO) = GetFullWorldSDFData();
        }

        /// <summary>
        /// Translates the world to a GPU-relevant format.
        /// </summary>
        /// <returns>ECS world information as data buffers</returns>
        public static (SDFWorldDTO, SDFPrimitiveObjectDTO[]) GetFullWorldSDFData()
        {
            SDFWorldDTO worldData = new SDFWorldDTO();
            List<SDFPrimitiveObjectDTO> sdfPrimitives = [];

            // Gather camera world data
            foreach (var (_, transform, camera) in W.QueryData<Transform, Camera>())
            {
                worldData.cameraOrigin = transform.position;
                worldData.cameraToWorld = camera.cameraToWorld;
                worldData.cameraInverseProj = camera.inverseProjectionMatrix;
                break; // Use first camera
            }

            // Gather and translate all primitives and effects
            foreach (var (id, transform) in W.QueryData<Transform>())
            {
                bool castShadows = false, receiveShadows = false;
                
                if (W.HasComponent<SoftShadows>(id))
                {
                    SoftShadows shadows = W.GetComponent<SoftShadows>(id)!;
                    castShadows = shadows.shadowCaster;
                    receiveShadows = shadows.shadowReceiver;
                }

                SDFPrimitiveObjectDTO curPrimitive = new SDFPrimitiveObjectDTO
                {
                    type = -1,
                    position = transform.position,
                    rotation = transform.rotation,
                    scaling = transform.scaling,
                    shadowEffects = new bool2(castShadows, receiveShadows)
                };
                if (W.HasComponent<SDFSphere>(id)) // Check sphere primitive
                {
                    SDFSphere sphere = W.GetComponent<SDFSphere>(id)!;
                    curPrimitive.type = 0; // Sphere type
                    curPrimitive.color = sphere.color;
                    curPrimitive.parameters = new float4(sphere.radius, 0f, 0f, 0f);
                }
                if (W.HasComponent<SDFBox>(id)) // Check box primitive
                {
                    SDFBox box = W.GetComponent<SDFBox>(id)!;
                    curPrimitive.type = 1; // Box type
                    curPrimitive.color = box.color;
                    curPrimitive.parameters = new float4(box.size.X, box.size.Y, box.size.Z, 0f);
                }
                if (W.HasComponent<SDFRoundedBox>(id)) // Check rounded box primitive
                {
                    SDFRoundedBox roundedBox = W.GetComponent<SDFRoundedBox>(id)!;
                    curPrimitive.type = 2; // Rounded box type
                    curPrimitive.color = roundedBox.color;
                    curPrimitive.parameters = new float4(roundedBox.size.X, roundedBox.size.Y, roundedBox.size.Z, roundedBox.bevel);
                }
                if (W.HasComponent<SDFTorus>(id)) // Check torus primitive
                {
                    SDFTorus torus = W.GetComponent<SDFTorus>(id)!;
                    curPrimitive.type = 3; // Torus type
                    curPrimitive.color = torus.color;
                    curPrimitive.parameters = new float4(torus.wholeRadius, torus.ringRadius, 0f, 0f);
                }
                if (W.HasComponent<SDFPyramid>(id)) // Check pyramid primitive
                {
                    SDFPyramid pyramid = W.GetComponent<SDFPyramid>(id)!;
                    curPrimitive.type = 4; // Pyramid type
                    curPrimitive.color = pyramid.color;
                    curPrimitive.parameters = new float4(pyramid.height, 0f, 0f, 0f);
                }

                // Space to find more SDF primitives in the future

                // Add the current primitive
                if (curPrimitive.type != -1) sdfPrimitives.Add(curPrimitive);
            }
            return (worldData, sdfPrimitives.ToArray());
        }
    }
}
