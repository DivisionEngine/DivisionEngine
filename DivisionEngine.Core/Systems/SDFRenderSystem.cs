using DivisionEngine.Components;
using DivisionEngine.Components.Lights;
using DivisionEngine.Components.SDFs;
using DivisionEngine.Components.SDFs.Effects;
using DivisionEngine.Components.SDFs.Primitives;
using DivisionEngine.Rendering;
using Math = DivisionEngine.MathLib.Math;
using Environment = DivisionEngine.Components.Environment;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Critial system in charge of packaging world render information before each render cycle.
    /// </summary>
    public class SDFRenderSystem : SystemBase
    {
        public const float EPSILON = 0.0001f;

        /// <summary>
        /// Prepared settings for the world information.
        /// </summary>
        public static SDFWorldDTO PreparedWorldDTO { get; private set; }

        /// <summary>
        /// Prepared settings for all SDF primitives in the world.
        /// </summary>
        public static SDFPrimitiveObjectDTO[] PreparedPrimitivesDTO { get; private set; } = [];

        /// <summary>
        /// Prepared settings for all SDF lights in the world.
        /// </summary>
        public static SDFLightDTO[] PreparedLightsDTO { get; private set; } = [];

        /// <summary>
        /// Called right before the world is rendered to screen.
        /// </summary>
        public override void Render() => (PreparedWorldDTO, PreparedPrimitivesDTO, PreparedLightsDTO) = GetFullWorldSDFData();

        /// <summary>
        /// Translates the world to a GPU-relevant format.
        /// </summary>
        /// <returns>ECS world information as data buffers</returns>
        public static (SDFWorldDTO, SDFPrimitiveObjectDTO[], SDFLightDTO[]) GetFullWorldSDFData()
        {
            SDFWorldDTO worldData = new SDFWorldDTO();
            List<SDFPrimitiveObjectDTO> sdfPrimitives = [];
            List<SDFLightDTO> sdfLights = [];

            // Gather camera data
            foreach (var (_, transform, camera) in W.QueryData<Transform, Camera>())
            {
                worldData.cameraOrigin = transform.position;
                worldData.cameraToWorld = camera.cameraToWorld;
                worldData.cameraInverseProj = camera.inverseProjectionMatrix;
                worldData.nearPlane = camera.nearClip;
                worldData.farPlane = camera.farClip;

                worldData.focusDistance = camera.focusDistance;
                worldData.focalLength = camera.focalLength;

                worldData.maxRaySteps = camera.maxRaySteps;
                worldData.maxShadowRaySteps = camera.maxShadowRaySteps;

                if (camera.enableDivisionDenoise) worldData.enableDivisionDenoise = 1;
                else worldData.enableDivisionDenoise = 0;

                if (camera.enableATrousDenoise) worldData.enableATrousDenoise = 1;
                else worldData.enableATrousDenoise = 0;

                worldData.divisionThreshold = camera.divisionDenoiseThreshold;
                worldData.divisionDomain = camera.divisionDenoiseDomain;
                worldData.aTrousStepCount = camera.aTrousStepCount;
                break; // Use first camera
            }

            // Gather environment data
            foreach (var (_, environment) in W.QueryData<Environment>())
            {
                worldData.backgroundColor = environment.backgroundColor;
                break; // Use first environment
            }

            // Gather and transform all lights
            foreach (var (id, transform) in W.QueryData<Transform>())
            {
                // Setup
                SDFLightDTO curLight = new SDFLightDTO
                {
                    type = -1,
                    position = transform.position,
                    rotation = transform.rotation,
                };
                
                // Effects
                if (W.HasComponent<DirectionalLight>(id))
                {
                    DirectionalLight light = W.GetComponent<DirectionalLight>(id)!;
                    curLight.color = light.color;
                    curLight.intensity = light.intensity;
                }
                if (W.HasComponent<PointLight>(id))
                {
                    PointLight light = W.GetComponent<PointLight>(id)!;
                    curLight.color = light.color;
                    curLight.intensity = light.intensity;
                }

                // Space to add more lights in the future

                // Add the current light
                if (curLight.type != -1) sdfLights.Add(curLight);
            }

            // Gather and transform all primitives and effects
            foreach (var (id, transform) in W.QueryData<Transform>())
            {
                // Setup
                SDFPrimitiveObjectDTO curPrimitive = new SDFPrimitiveObjectDTO
                {
                    type = -1,
                    position = transform.position,
                    rotation = transform.rotation,
                    scaling = new float3(
                        Math.Max(1f / transform.scaling.X, EPSILON),
                        Math.Max(1f / transform.scaling.Y, EPSILON),
                        Math.Max(1f / transform.scaling.Z, EPSILON)),
                };

                // Material
                if (W.HasComponent<SDFMaterial>(id))
                {
                    SDFMaterial mat = W.GetComponent<SDFMaterial>(id)!;
                    curPrimitive.color = mat.albedoColor;
                    curPrimitive.metallic = mat.metallic;
                    curPrimitive.roughness = mat.roughness;
                    curPrimitive.specular = mat.specular;
                    curPrimitive.ior = mat.ior;
                    curPrimitive.ao = mat.ambientOcclusion;
                    curPrimitive.reflectance = mat.reflectance;
                }

                // Effects
                if (W.HasComponent<SoftShadows>(id))
                {
                    SoftShadows shadows = W.GetComponent<SoftShadows>(id)!;
                    curPrimitive.shadowEffects = new bool2(shadows.shadowCaster, shadows.shadowReceiver);
                    curPrimitive.shadowDistances = new float2(shadows.minDistance, shadows.maxDistance);
                }
                if (W.HasComponent<Reflections>(id))
                {
                    Reflections reflect = W.GetComponent<Reflections>(id)!;
                    if (reflect.hasReflections) curPrimitive.hasReflection = 1;
                    else curPrimitive.hasReflection = 0;
                    if (reflect.reflectionShadows) curPrimitive.reflectionShadows = 1;
                    else curPrimitive.reflectionShadows = 0;
                    curPrimitive.reflectRayStepFalloff = reflect.rayStepsFalloff;
                    curPrimitive.reflectionMaxBounces = reflect.maxBounces;
                }
                if (W.HasComponent<Refractions>(id))
                {
                    Refractions refract = W.GetComponent<Refractions>(id)!;
                    if (refract.hasRefractions) curPrimitive.hasRefraction = 1;
                    else curPrimitive.hasRefraction = 0;
                    curPrimitive.absorptionColor = refract.absorptionColor;
                    curPrimitive.refractionMaxSteps = refract.maxRaySteps;
                    curPrimitive.refractMaxRecursion = refract.maxRecursionTraces;
                }

                // Primitives
                if (W.HasComponent<SDFSphere>(id)) // Check sphere primitive
                {
                    SDFSphere sphere = W.GetComponent<SDFSphere>(id)!;
                    curPrimitive.type = 0; // Sphere type
                    curPrimitive.parameters = new float4(sphere.radius, 0f, 0f, 0f);
                }
                if (W.HasComponent<SDFBox>(id)) // Check box primitive
                {
                    SDFBox box = W.GetComponent<SDFBox>(id)!;
                    curPrimitive.type = 1; // Box type
                    curPrimitive.parameters = new float4(box.size.X, box.size.Y, box.size.Z, 0f);
                }
                if (W.HasComponent<SDFRoundedBox>(id)) // Check rounded box primitive
                {
                    SDFRoundedBox roundedBox = W.GetComponent<SDFRoundedBox>(id)!;
                    curPrimitive.type = 2; // Rounded box type
                    curPrimitive.parameters = new float4(roundedBox.size.X, roundedBox.size.Y, roundedBox.size.Z, roundedBox.bevel);
                }
                if (W.HasComponent<SDFTorus>(id)) // Check torus primitive
                {
                    SDFTorus torus = W.GetComponent<SDFTorus>(id)!;
                    curPrimitive.type = 3; // Torus type
                    curPrimitive.parameters = new float4(torus.wholeRadius, torus.ringRadius, 0f, 0f);
                }
                if (W.HasComponent<SDFPyramid>(id)) // Check pyramid primitive
                {
                    SDFPyramid pyramid = W.GetComponent<SDFPyramid>(id)!;
                    curPrimitive.type = 4; // Pyramid type
                    curPrimitive.parameters = new float4(pyramid.height, 0f, 0f, 0f);
                }
                if (W.HasComponent<SDFPlane>(id)) // Check plane primitive
                {
                    SDFPlane plane = W.GetComponent<SDFPlane>(id)!;
                    curPrimitive.type = 5; // Plane type
                    curPrimitive.parameters = new float4(plane.normal.X, plane.normal.Y, plane.normal.Z, plane.height);
                }
                if (W.HasComponent<SDFCylinder>(id)) // Check cylinder primitive
                {
                    SDFCylinder cylinder = W.GetComponent<SDFCylinder>(id)!;
                    curPrimitive.type = 6; // Cylinder type
                    curPrimitive.parameters = new float4(cylinder.radius, cylinder.height, 0f, 0f);
                }
                if (W.HasComponent<SDFCapsule>(id)) // Check capsule primitive
                {
                    SDFCapsule capsule = W.GetComponent<SDFCapsule>(id)!;
                    curPrimitive.type = 7; // Capsule type
                    curPrimitive.parameters = new float4(capsule.radius, capsule.height, 0f, 0f);
                }
                if (W.HasComponent<SDFCone>(id)) // Check cone primitive
                {
                    SDFCone cone = W.GetComponent<SDFCone>(id)!;
                    curPrimitive.type = 8; // Cone type
                    curPrimitive.parameters = new float4(cone.cone.X, cone.cone.Y, cone.height, 0f);
                }

                // Space to find more SDF primitives in the future

                // Add the current primitive
                if (curPrimitive.type != -1) sdfPrimitives.Add(curPrimitive);
            }

            return (worldData, sdfPrimitives.ToArray(), sdfLights.ToArray());
        }
    }
}
