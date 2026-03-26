//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components;
using DivisionEngine.Components.Lights;
using DivisionEngine.Components.SDFs;
using DivisionEngine.Components.SDFs.Effects;
using DivisionEngine.Components.SDFs.Primitives;
using DivisionEngine.MathLib;
using DivisionEngine.Rendering;
using Environment = DivisionEngine.Components.Environment;
using Math = DivisionEngine.MathLib.Math;

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
                // Camera transform
                worldData.cameraOrigin = transform.position;
                worldData.camForward = transform.Forward;
                worldData.camRight = transform.Right;
                worldData.camUp = transform.Up;

                // Camera distances
                worldData.nearPlane = camera.nearClip;
                worldData.farPlane = camera.farClip;
                worldData.camScreenDist = CameraSystem.FovToScreenDistance(camera); // Calc camera screen distance
                
                // Depth of field
                worldData.focusDistance = camera.focusDistance;
                worldData.focalLength = camera.focalLength;

                // Ray step counts
                worldData.maxRaySteps = camera.maxRaySteps;
                worldData.maxShadowRaySteps = camera.maxShadowRaySteps;

                // Denoising
                if (camera.enableDivisionDenoise) worldData.enableDivisionDenoise = 1;
                else worldData.enableDivisionDenoise = 0;
                if (camera.enableATrousDenoise) worldData.enableATrousDenoise = 1;
                else worldData.enableATrousDenoise = 0;
                worldData.divisionThreshold = camera.divisionDenoiseThreshold;
                worldData.divisionDomain = camera.divisionDenoiseDomain;
                worldData.aTrousStepCount = camera.aTrousStepCount;
                break; // Use first camera
            }

            // Gather fog data
            foreach (var (_, fog) in W.QueryData<VolumetricFog>())
            {
                worldData.fogDensity = fog.density;
                worldData.fogColor = fog.color;
                worldData.fogAbsorption = fog.absorption;
                worldData.fogScattering = fog.scattering;
                worldData.fogAnisotropy = fog.anisotropy;
                break; // Use first fog component
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
                    position = transform.position,
                    rotation = transform.rotation,
                };

                // Lights
                if (W.HasComponent<DirectionalLight>(id))
                {
                    DirectionalLight light = W.GetComponent<DirectionalLight>(id)!;
                    curLight.type = 0;
                    curLight.color = light.color;
                    curLight.intensity = light.intensity;
                    sdfLights.Add(curLight);
                }
                else if (W.HasComponent<PointLight>(id))
                {
                    PointLight light = W.GetComponent<PointLight>(id)!;
                    curLight.type = 1;
                    curLight.color = light.color;
                    curLight.intensity = light.intensity;
                    curLight.radius = light.radius;
                    sdfLights.Add(curLight);
                }

                // Space to add more lights in the future
            }

            // Gather and transform all primitives and effects
            foreach (var (id, transform) in W.QueryData<Transform>())
            {
                // Setup
                SDFPrimitiveObjectDTO curPrimitive = new SDFPrimitiveObjectDTO
                {
                    entityId = id,
                    type = -1,
                    position = transform.position,
                    rotation = transform.rotation,
                    scaling = new float3(
                        Math.Max(1f / transform.scaling.X, EPSILON),
                        Math.Max(1f / transform.scaling.Y, EPSILON),
                        Math.Max(1f / transform.scaling.Z, EPSILON)),
                    color = new float4(1f, 0f, 1f, 1f), // Default render color if no material active
                };

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

                // Material
                if (W.HasComponent<SDFMaterial>(id))
                {
                    SDFMaterial mat = W.GetComponent<SDFMaterial>(id)!;
                    curPrimitive.color = mat.albedoColor;
                    curPrimitive.metallic = mat.metallic;
                    curPrimitive.roughness = mat.roughness;
                    curPrimitive.specular = mat.specular;
                    curPrimitive.ior = mat.ior;
                    curPrimitive.aoValues = new float3(mat.ambientOcclusion, mat.ambientRange, mat.ambientFalloff);
                    curPrimitive.reflectance = mat.reflectance;

                    // Precalculate material values
                    float s2016 = 0.16f * mat.specular * mat.specular;
                    float3 f0 = new float3(s2016, s2016, s2016);
                    curPrimitive.f0_reflectance = new float3(
                        Math.Lerp(f0.X, mat.albedoColor.X, mat.metallic),
                        Math.Lerp(f0.Y, mat.albedoColor.Y, mat.metallic),
                        Math.Lerp(f0.Z, mat.albedoColor.Z, mat.metallic));
                    if (curPrimitive.hasRefraction == 1) curPrimitive.f0_dielectric = Math.Pow((mat.ior - 1.0f) / (mat.ior + 1.0f), 2.0f);
                }

                // Primitives
                if (W.HasComponent<SDFSphere>(id)) // Check sphere primitive
                {
                    SDFSphere sphere = W.GetComponent<SDFSphere>(id)!;
                    curPrimitive.type = 0; // Sphere type
                    curPrimitive.parameters = new float4(sphere.radius, 0f, 0f, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFBox>(id)) // Check box primitive
                {
                    SDFBox box = W.GetComponent<SDFBox>(id)!;
                    curPrimitive.type = 1; // Box type
                    curPrimitive.parameters = new float4(box.size.X, box.size.Y, box.size.Z, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFRoundedBox>(id)) // Check rounded box primitive
                {
                    SDFRoundedBox roundedBox = W.GetComponent<SDFRoundedBox>(id)!;
                    curPrimitive.type = 2; // Rounded box type
                    curPrimitive.parameters = new float4(roundedBox.size.X, roundedBox.size.Y, roundedBox.size.Z, roundedBox.bevel);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFTorus>(id)) // Check torus primitive
                {
                    SDFTorus torus = W.GetComponent<SDFTorus>(id)!;
                    curPrimitive.type = 3; // Torus type
                    curPrimitive.parameters = new float4(torus.wholeRadius, torus.ringRadius, 0f, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFPyramid>(id)) // Check pyramid primitive
                {
                    SDFPyramid pyramid = W.GetComponent<SDFPyramid>(id)!;
                    curPrimitive.type = 4; // Pyramid type
                    curPrimitive.parameters = new float4(pyramid.height, 0f, 0f, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFPlane>(id)) // Check plane primitive
                {
                    SDFPlane plane = W.GetComponent<SDFPlane>(id)!;
                    curPrimitive.type = 5; // Plane type
                    curPrimitive.parameters = new float4(plane.normal.X, plane.normal.Y, plane.normal.Z, plane.height);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFCylinder>(id)) // Check cylinder primitive
                {
                    SDFCylinder cylinder = W.GetComponent<SDFCylinder>(id)!;
                    curPrimitive.type = 6; // Cylinder type
                    curPrimitive.parameters = new float4(cylinder.radius, cylinder.height, 0f, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFCapsule>(id)) // Check capsule primitive
                {
                    SDFCapsule capsule = W.GetComponent<SDFCapsule>(id)!;
                    curPrimitive.type = 7; // Capsule type
                    curPrimitive.parameters = new float4(capsule.radius, capsule.height, 0f, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }
                if (W.HasComponent<SDFCone>(id)) // Check cone primitive
                {
                    SDFCone cone = W.GetComponent<SDFCone>(id)!;
                    curPrimitive.type = 8; // Cone type
                    curPrimitive.parameters = new float4(cone.cone.X, cone.cone.Y, cone.height, 0f);
                    sdfPrimitives.Add(curPrimitive);
                }

                // Space to find more SDF primitives in the future
            }

            return (worldData, sdfPrimitives.ToArray(), sdfLights.ToArray());
        }
    }
}
