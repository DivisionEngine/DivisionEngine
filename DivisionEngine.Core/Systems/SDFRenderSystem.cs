using DivisionEngine.Components;
using DivisionEngine.Components.SDFs;
using DivisionEngine.Components.SDFs.Effects;
using DivisionEngine.Rendering;
using Silk.NET.Maths;

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
                if (W.HasComponent<SDFSphere>(id)) // Gather sphere primitives
                {
                    SDFSphere sphere = W.GetComponent<SDFSphere>(id)!;
                    sdfPrimitives.Add(new SDFPrimitiveObjectDTO
                    {
                        type = 0, // Sphere type
                        color = sphere.color,
                        position = transform.position,
                        rotation = transform.rotation,
                        scaling = transform.scaling,
                        parameters = new float4(sphere.radius, 0f, 0f, 0f),
                        shadowEffects = new bool2(castShadows, receiveShadows)
                    });
                }
                if (W.HasComponent<SDFBox>(id)) // Gather box primitives
                {
                    SDFBox box = W.GetComponent<SDFBox>(id)!;
                    sdfPrimitives.Add(new SDFPrimitiveObjectDTO
                    {
                        type = 1, // Box type
                        color = box.color,
                        position = transform.position,
                        rotation = transform.rotation,
                        scaling = transform.scaling,
                        parameters = new float4(box.size.X, box.size.Y, box.size.Z, 0f),
                        shadowEffects = new bool2(castShadows, receiveShadows)
                    });
                }
                if (W.HasComponent<SDFRoundedBox>(id)) // Gather rounded box primitives
                {
                    SDFRoundedBox roundedBox = W.GetComponent<SDFRoundedBox>(id)!;
                    sdfPrimitives.Add(new SDFPrimitiveObjectDTO
                    {
                        type = 2, // Rounded box type
                        color = roundedBox.color,
                        position = transform.position,
                        rotation = transform.rotation,
                        scaling = transform.scaling,
                        parameters = new float4(roundedBox.size.X, roundedBox.size.Y, roundedBox.size.Z, roundedBox.bevel),
                        shadowEffects = new bool2(castShadows, receiveShadows)
                    });
                }
                if (W.HasComponent<SDFTorus>(id)) // Gather torus primitives
                {
                    SDFTorus torus = W.GetComponent<SDFTorus>(id)!;
                    sdfPrimitives.Add(new SDFPrimitiveObjectDTO
                    {
                        type = 3, // Torus type
                        color = torus.color,
                        position = transform.position,
                        rotation = transform.rotation,
                        scaling = transform.scaling,
                        parameters = new float4(torus.wholeRadius, torus.ringRadius, 0f, 0f),
                        shadowEffects = new bool2(castShadows, receiveShadows)
                    });
                }
                // Space to add more SDF primitives in the future
            }
            return (worldData, sdfPrimitives.ToArray());
        }
    }
}
