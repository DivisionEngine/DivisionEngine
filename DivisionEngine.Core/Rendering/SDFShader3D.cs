//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;
using DivisionEngine.Rendering;

namespace DivisionEngine
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFShader3D(
        float width,
        float height,
        float aspect,
        int frameCount,
        int debugMode,
        SDFWorldDTO worldData,
        ReadWriteTexture2D<float4> texture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadWriteBuffer<uint2> entityIdBuffer,
        ReadOnlyBuffer<SDFObjectDTO> sdfObjects,
        ReadOnlyBuffer<SDFLightDTO> lights) : IComputeShader
    {
        // Main constants
        const float EPSILON = 0.0001f;
        const float PI = 3.141592654f;
        const float RECIPROCAL_PI = 1f / PI;
        const float MIN_TRAVERSE_DIST = 100000000.0f;

        // Reflection constants
        const int SAMPLES_PER_PIXEL = 2;
        const float MIN_REFLECTION_CHANCE = 0.01f;
        const float MIN_THROUGHPUT = 0.01f;
        const float REFLECTION_BIAS = 2f; // Multiplier for normal offset

        /// <summary>
        /// Picks a random color from an integer.
        /// </summary>
        /// <param name="id">Object ID</param>
        /// <returns>Random hashed color</returns>
        private static float3 IntToColor(uint id)
        {
            // Mix the bits using prime numbers
            uint hash = id;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;

            // Convert to float in [0,1] range
            float r = (hash & 0xFF) / 255.0f;
            float g = ((hash >> 8) & 0xFF) / 255.0f;
            float b = ((hash >> 16) & 0xFF) / 255.0f;

            // Ensure minimum brightness and saturation
            return Hlsl.Max(new float3(r, g, b), 0.2f);
        }

        private float AdaptiveEpsilon(float td)
        {
            float pixelSize = Hlsl.Max(td * worldData.camScreenDist / height, td * worldData.camScreenDist / width);
            return Hlsl.Max(EPSILON, pixelSize); // Could use half pixel size
        }

        // Quaternion rotation
        private static float3 RotateVector(float3 v, float4 r)
        {
            float3 qv = r.XYZ;
            float3 t = 2.0f * Hlsl.Cross(qv, v);
            return v + r.W * t + Hlsl.Cross(qv, t);
        }

        private float3 GetCameraRayDirNew(float2 uv)
        {
            float px = uv.X * aspect * worldData.camScreenDist;
            float py = uv.Y * worldData.camScreenDist;
            float3 rayDir = worldData.camForward + worldData.camRight * px + worldData.camUp * py;
            return Hlsl.Normalize(rayDir);
        }

        private static float SphereSDF(float3 pt, float r)
        {
            //float3 s = new float3(8, 8, 8);
            //float3 l = new float3(100, 1, 100);
            //float3 q = pt - s * Hlsl.Clamp(Hlsl.Round(pt / s), -l, l);
            return Hlsl.Length(pt) - r;
        }

        private static float BoxSDF(float3 pt, float3 size)
        {
            float3 q = Hlsl.Abs(pt) - size;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f);

            //float3 s = new float3(8, 8, 8);
            //float3 l = new float3(10000, 6, 10000);
            //float3 q = pt - s * Hlsl.Clamp(Hlsl.Round(pt / s), -l, l);

            //float sd1 = TorusSDF(q, size.XY);
            //float sd2 = PyramidSDF(q, size.X);

            //return Hlsl.Lerp(sd1, sd2, Hlsl.Saturate(worldData.fogAnisotropy));
        }

        private static float RoundedBoxSDF(float3 pt, float3 size, float r)
        {
            float3 q = Hlsl.Abs(pt) - size + r;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f) - r;
        }

        private static float TorusSDF(float3 pt, float2 tr)
        {
            float2 q = new float2(Hlsl.Length(pt.XZ) - tr.X, pt.Y);
            return Hlsl.Length(q) - tr.Y;
        }

        private static float PyramidSDF(float3 pt, float h)
        {
            float m2 = h * h + 0.25f;

            pt.XZ = Hlsl.Abs(pt.XZ);
            pt.XZ = (pt.Z > pt.X) ? pt.ZX : pt.XZ;
            pt.XZ -= 0.5f;

            float3 q = new float3(pt.Z, h * pt.Y - 0.5f * pt.X, h * pt.X + 0.5f * pt.Y);

            float s = Hlsl.Max(-q.X, 0.0f);
            float t = Hlsl.Saturate((q.Y - 0.5f * pt.Z) / (m2 + 0.25f));

            float a = m2 * (q.X + s) * (q.X + s) + q.Y * q.Y;
            float b = m2 * (q.X + 0.5f * t) * (q.X + 0.5f * t) + (q.Y - m2 * t) * (q.Y - m2 * t);

            float d2 = Hlsl.Min(q.Y, -q.X * m2 - q.Y * 0.5f) > 0.0f ? 0.0f : Hlsl.Min(a, b);

            return Hlsl.Sqrt((d2 + q.Z * q.Z) / m2) * Hlsl.Sign(Hlsl.Max(q.Z, -pt.Y));
        }

        private static float PlaneSDF(float3 pt, float3 n, float h)
        {
            return Hlsl.Dot(pt, Hlsl.Normalize(n)) + h;
        }

        // Bound not exact, for performance
        private static float ConeSDF(float3 pt, float2 c, float h)
        {
            float q = Hlsl.Length(pt.XZ);
            return Hlsl.Max(Hlsl.Dot(c.XY, new float2(q, pt.Y)), -h - pt.Y);
        }

        // Vertical version, for performance
        private static float CylinderSDF(float3 pt, float r, float h)
        {
            float2 d = Hlsl.Abs(new float2(Hlsl.Length(pt.XZ), pt.Y)) - new float2(r, h);
            return Hlsl.Min(Hlsl.Max(d.X, d.Y), 0.0f) + Hlsl.Length(Hlsl.Max(d, 0.0f));
        }

        // Vertical version, for performance
        private static float CapsuleSDF(float3 pt, float r, float h)
        {
            pt.Y -= Hlsl.Clamp(pt.Y, 0.0f, h);
            return Hlsl.Length(pt) - r;
        }

        // ------------------
        // Terrain Generation
        // ------------------

        private float TerrainSDF(float3 pt, float size, float heightScale, float persistence, float lacunarity)
        {
            float height = SimplexNoise.FBM2D(pt.XZ / size, 4, persistence, lacunarity);
            float grassHeight = SimplexNoise.FBM2D(pt.XZ / 10f + new float2(103, -1025), 2, persistence, lacunarity);
            float terrainHeight = height * heightScale - (heightScale / 2f);

            // Repeating balls
            float spacing = 1f;
            float ballRadius = 0.1f;

            // Get position within repeating cell
            float2 cellPos = new float2(
                Hlsl.Fmod(Hlsl.Abs(pt.X), spacing) - spacing * 0.5f,
                Hlsl.Fmod(Hlsl.Abs(pt.Z), spacing) - spacing * 0.5f
            );

            // Ball at each grid point (placed on terrain surface)
            float3 grassLocal = new float3(cellPos.X, pt.Y - terrainHeight, cellPos.Y);
            float grass = CapsuleSDF(grassLocal, ballRadius, grassHeight * 2f);

            // Box terrain - for the future
            //float3 q = Hlsl.Abs(pt) - new float3(size, 0f, size);
            //if (pt.Y > 0f) q.Y -= terrainHeight - (heightScale / 3f);
            //float terrain = Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f);

            float terrain = pt.Y - terrainHeight;
            return Hlsl.Min(terrain, grass);
        }

        /// <summary>
        /// Calculates the SDF distance for the world at a point.
        /// </summary>
        /// <param name="point">World position to evaluate</param>
        /// <param name="shadowCastCheck">Should the tracer verify shadow casters</param>
        /// <returns>Float2 representing the min distance, and closest object</returns>
        private float WorldSDF(float3 point, bool shadowCastCheck, uint excludeID, out int closest)
        {
            float minDist = MIN_TRAVERSE_DIST;

            closest = -1;
            for (int i = 0; i < sdfObjects.Length; i++)
            {
                SDFObjectDTO curSDF = sdfObjects[i];
                if (shadowCastCheck && !curSDF.shadowEffects.X) continue;
                if (sdfObjects[i].entityId == excludeID) continue; // Exclude to get second-closest object
                float3 scaling = curSDF.scaling;
                float3 curPoint = point - curSDF.position; // Transform SDF
                curPoint = RotateVector(curPoint, curSDF.rotation); // Rotate SDF
                curPoint *= scaling;

                // Scale distance function
                float dist = Hlsl.Min(scaling.X, Hlsl.Min(scaling.Y, scaling.Z));
                if (curSDF.type == 0) // Adds sphere SDFs
                    dist *= SphereSDF(curPoint, curSDF.parameters.X);
                else if (curSDF.type == 1) // Adds box SDFs
                    dist *= BoxSDF(curPoint, curSDF.parameters.XYZ);
                else if (curSDF.type == 2) // Adds rounded box SDFs
                    dist *= RoundedBoxSDF(curPoint, curSDF.parameters.XYZ, curSDF.parameters.W);
                else if (curSDF.type == 3) // Adds torus SDFs
                    dist *= TorusSDF(curPoint, curSDF.parameters.XY);
                else if (curSDF.type == 4) // Adds pyramid SDFs
                    dist *= PyramidSDF(curPoint, curSDF.parameters.X);
                else if (curSDF.type == 5) // Adds plane SDFs
                    dist *= PlaneSDF(curPoint, curSDF.parameters.XYZ, curSDF.parameters.W);
                else if (curSDF.type == 6) // Adds cylinder SDFs
                    dist *= CylinderSDF(curPoint, curSDF.parameters.X, curSDF.parameters.Y);
                else if (curSDF.type == 7) // Adds capsule SDFs
                    dist *= CapsuleSDF(curPoint, curSDF.parameters.X, curSDF.parameters.Y);
                else if (curSDF.type == 8) // Adds cone SDFs
                    dist *= ConeSDF(curPoint, curSDF.parameters.XY, curSDF.parameters.Z);
                else if (curSDF.type == 9) // Adds terrain SDFs
                    dist *= TerrainSDF(curPoint, curSDF.parameters.X, curSDF.parameters.Y, curSDF.parameters.Z, curSDF.parameters.W);
                else // Default to sphere SDF
                    dist *= SphereSDF(curPoint, curSDF.parameters.X);

                dist *= curSDF.stepBias;
                if (Hlsl.Abs(dist) < minDist)
                {
                    closest = i;
                    minDist = dist;
                }
            }

            // Return packaged minimum SDF distance and closest object index
            return minDist;
        }

        // -------
        // Normals
        // -------

        /// <summary>
        /// Very fast high quality normal calculation (4 samples).
        /// </summary>
        /// <param name="pos">Hit position</param>
        /// <returns>World normal vector</returns>
        private float3 FastNormal(float3 pos) // for function f(p)
        {
            float h = EPSILON * 50; // replace by an appropriate value
            float2 k = new float2(1f, -1f);
            return Hlsl.Normalize(k.XYY * WorldSDF(pos + k.XYY * h, false, uint.MaxValue, out _) +
                              k.YYX * WorldSDF(pos + k.YYX * h, false, uint.MaxValue, out _) +
                              k.YXY * WorldSDF(pos + k.YXY * h, false, uint.MaxValue, out _) +
                              k.XXX * WorldSDF(pos + k.XXX * h, false, uint.MaxValue, out _));
        }

        // Platform compliant version
        //private float3 FastNormal(float3 pos)
        //{
        //    float3 n = new float3(0f, 0f, 0f);
        //    for (int i = 0; i < 4; i++)
        //    {
        //        float3 e = 0.5773f * (2f * new float3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1f);
        //        n += e * WorldSDF(pos + EPSILON * 50 * e, false, uint.MaxValue, out _);
        //        if (n.X + n.Y + n.Z > 100f) break;
        //    }
        //    return Hlsl.Normalize(n);
        //}

        /// <summary>
        /// 6-Sample normal calculation.
        /// </summary>
        /// <param name="p">Point to sample at</param>
        /// <returns>High quality normal</returns>
        private float3 StableNormal(float3 p)
        {
            float e = EPSILON * 2.0f;
            float dx = WorldSDF(p + new float3(e, 0, 0), false, uint.MaxValue, out _) -
                       WorldSDF(p - new float3(e, 0, 0), false, uint.MaxValue, out _);
            float dy = WorldSDF(p + new float3(0, e, 0), false, uint.MaxValue, out _) -
                       WorldSDF(p - new float3(0, e, 0), false, uint.MaxValue, out _);
            float dz = WorldSDF(p + new float3(0, 0, e), false, uint.MaxValue, out _) -
                       WorldSDF(p - new float3(0, 0, e), false, uint.MaxValue, out _);
            return Hlsl.Normalize(new float3(dx, dy, dz));
        }

        // --------------------
        // Lighting Calculation
        // --------------------

        // New soft-shadow technique:
        // Reference: https://iquilezles.org/articles/rmshadows/
        // New Version: https://www.shadertoy.com/view/tscSRS
        private float SoftShadow2(float3 point, float3 dir, float start, float end, out int closestObj)
        {
            float depth = start, dist;
            float shadow = 1f;
            closestObj = -1;

            for (int i = 0; i < worldData.maxShadowRaySteps; ++i)
            {
                dist = WorldSDF(point + depth * dir, true, uint.MaxValue, out closestObj);
                if (depth > end || shadow < -1f) break;

                shadow = Hlsl.Min(shadow, worldData.shadowScale * dist / depth);
                depth += Hlsl.Clamp(dist, 0.01f, 10f);
            }

            shadow = Hlsl.Max(shadow, -1f);
            return Hlsl.SmoothStep(-1f, 0f, shadow);
        }

        /// <summary>
        /// Calculate lighting contribution from all lights
        /// </summary>
        private float3 CalculateLighting(float3 hitPoint, float3 normal, float3 viewDir, SDFObjectDTO material)
        {
            float3 totalLight = float3.Zero;
            float lightShadow = 1f;

            for (int i = 0; i < lights.Length; i++)
            {
                SDFLightDTO light = lights[i];
                if (light.type == 0) // Directional light
                {
                    float3 lightDir = Hlsl.Normalize(RotateVector(new float3(0, 0, -1), light.rotation)); // Directional lights use direction vector
                    float3 lightColor = light.color.RGB * light.intensity;

                    float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
                    if (NoL <= 0f) continue;

                    // Calculate shadow for directional light
                    if (material.shadowEffects.Y)
                    {
                        float3 shadowOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS;
                        lightShadow = Hlsl.Min(SoftShadow2(shadowOrigin, lightDir,
                            material.shadowDistances.X, material.shadowDistances.Y, out _), lightShadow);
                    }

                    // Calculate BRDF here now
                    float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal,
                        material.metallic, material.roughness * material.roughness,
                        material.color.RGB, material.specular);
                    totalLight += brdf * lightColor * NoL * lightShadow;
                }
                else if (light.type == 1) // Point light
                {
                    float3 lightVec = light.position - hitPoint;
                    float distance = Hlsl.Length(lightVec);
                    float3 lightDir = lightVec / distance;
                    float attenuation = 1f / (distance * distance);

                    // Apply radius falloff
                    float radiusFactor = Hlsl.Saturate(1f - (distance / light.radius));
                    attenuation *= radiusFactor;

                    float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
                    if (NoL <= 0f || attenuation <= 0f) continue;

                    float3 lightColor = light.color.RGB * light.intensity * attenuation;

                    // Simplified shadow for point lights
                    if (material.shadowEffects.Y && distance < light.radius * 2f)
                    {
                        // Basic point light shadow (could be optimized)
                        float3 shadowOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS;
                        lightShadow = Hlsl.Min(SoftShadow2(shadowOrigin, lightDir,
                            material.shadowDistances.X, distance, out _), lightShadow);
                    }

                    // Calculate point light BRDF
                    float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal,
                        material.metallic, material.roughness * material.roughness,
                        material.color.RGB, material.specular);
                    totalLight += brdf * lightColor * NoL * lightShadow;
                }
            }

            return totalLight;
        }

        /// <summary>
        /// Calculates ambient occlusion by getting the distance to the second-closest SDF object.
        /// </summary>
        /// <param name="hitPoint">Initial hit point</param>
        /// <param name="normal">Initial hit normal</param>
        /// <param name="entity">Initial entity hit</param>
        /// <returns>Occlusion value at hit point</returns>
        private float CalculateAO(float3 hitPoint, float3 normal, SDFObjectDTO entity)
        {
            float3 samplePoint = hitPoint + normal * EPSILON; // Normal vector offset
            float worldDist = WorldSDF(samplePoint, false, entity.entityId, out _); // Get distance to the next closest object
            float occlusionRadius = entity.aoValues.Y;
            if (worldDist >= occlusionRadius) return 1f; // No object found within radius
            float occlusion = 1f - Hlsl.Saturate(worldDist / occlusionRadius);
            occlusion = Hlsl.Pow(occlusion, entity.aoValues.Z);
            return 1f - occlusion;
        }

        // ------------------------------
        // New Correct PBR BRDF Functions
        // ------------------------------

        private static float3 FresnelSchlick(float cosTheta, float3 f0)
        {
            return f0 + (float3.One - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        /// <summary>
        /// Calculates the diffuse factor for GGX.
        /// </summary>
        /// <param name="NoH">Normal dot Halfway</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>Diffuse GGX factor</returns>
        private static float D_GGX(float NoH, float alpha)
        {
            float alpha2 = alpha * alpha;
            float NoH2 = NoH * NoH;
            float b = NoH2 * (alpha2 - 1f) + 1f;
            return alpha2 * RECIPROCAL_PI / (b * b);
        }

        /// <summary>
        /// Combines GGX Schlick functions for difference vectors.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="NoL">Normal dot Light</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>G Smith value</returns>
        private static float GSmith(float NoV, float NoL, float alpha)
        {
            return G1_GGX_Schlick(NoL, alpha) * G1_GGX_Schlick(NoV, alpha);
        }

        /// <summary>
        /// Calculates the GGX Schlick function.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>G1 factor for GGX Schlick</returns>
        private static float G1_GGX_Schlick(float NoV, float alpha)
        {
            float k = alpha / 2f;
            return Hlsl.Max(NoV, EPSILON) / (NoV * (1f - k) + k);
        }

        /// <summary>
        /// Fresnel equation used by Disney.
        /// </summary>
        /// <param name="cosTheta">Cos theta angle</param>
        /// <param name="f0">Fresnel term</param>
        /// <param name="f90">90 degree fresnel term</param>
        /// <returns>Fresnel value</returns>
        private static float FresnelSchlick90(float cosTheta, float f0, float f90)
        {
            return f0 + (f90 - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        /// <summary>
        /// Calculates the diffuse factor for the BRDF using Disney's method.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="NoL">Normal dot Light</param>
        /// <param name="VoH">View dot Halfway</param>
        /// <param name="alpha">Roughness value squared</param>
        /// <returns>Disney diffuse factor for BRDF</returns>
        private static float DisneyDiffuseFactor(float NoV, float NoL, float VoH, float alpha)
        {
            float f90 = 0.5f + 2f * alpha * VoH * VoH;
            float F_In = FresnelSchlick90(NoL, 1f, f90);
            float F_Out = FresnelSchlick90(NoV, 1f, f90);
            return F_In * F_Out;
        }

        /// <summary>
        /// The bi-directional reflectance distributionfunction using Cook-Torrance.
        /// </summary>
        /// <param name="lightDir">Light direction</param>
        /// <param name="viewDir">View direction</param>
        /// <param name="normal">Normal vector</param>
        /// <param name="metallic">Metallic amount 0 or 1</param>
        /// <param name="roughAlpha">Roughness * Roughness</param>
        /// <param name="baseCol">Base color of material</param>
        /// <param name="reflectance">Reflectance level of material</param>
        /// <returns>BRDF output value</returns>
        private static float3 BRDFMicrofacetFunction(float3 lightDir, float3 viewDir, float3 normal,
            float metallic, float roughAlpha, float3 baseCol, float reflectance)
        {
            float3 halfwayDir = Hlsl.Normalize(viewDir + lightDir);
            float NoV = Hlsl.Saturate(Hlsl.Dot(normal, viewDir));
            float NoL = Hlsl.Saturate(Hlsl.Dot(normal, lightDir));
            float VoH = Hlsl.Saturate(Hlsl.Dot(viewDir, halfwayDir));
            float NoH = Hlsl.Saturate(Hlsl.Dot(normal, halfwayDir));

            float3 f0 = float3.One * 0.16f * reflectance * reflectance;
            f0 = Hlsl.Lerp(f0, baseCol, new float3(metallic, metallic, metallic));

            float3 F = FresnelSchlick(VoH, f0);
            float D = D_GGX(NoH, roughAlpha);
            float G = GSmith(NoV, NoL, roughAlpha);

            // Add epsilon to denominator to prevent division by near-zero
            float denominator = 4f * Hlsl.Max(NoV * NoL, EPSILON);
            float3 specular = F * D * G / denominator;

            // Clamp specular to prevent fireflies
            specular = Hlsl.Min(specular, 20.0f); // Limit max brightness

            // Diffuse
            float3 rhoD = baseCol;
            rhoD *= DisneyDiffuseFactor(NoV, NoL, VoH, roughAlpha);
            //rhoD *= 1f - metallic;
            float3 diff = rhoD * RECIPROCAL_PI;

            // Clamp final BRDF result
            return Hlsl.Min(diff + specular, 100.0f); // Hard cap to prevent explosions
        }

        /// <summary>
        /// Calculates fresnel reflectance for dielectrics (glass, water, etc.)
        /// </summary>
        private static float SimpleFresnelDielectric(float cosθ, float f0)
        {
            // Schlick approximation (close enough for most cases)
            return f0 + (1.0f - f0) * Hlsl.Pow(1.0f - cosθ, 5.0f);
        }

        // Reflections functions:

        private static uint HaltonHash(uint x)
        {
            x = x ^ 61 ^ (x >> 16);
            x += x << 3;
            x ^= x >> 4;
            x *= 0x27d4eb2d;
            x ^= x >> 15;
            return x;
        }

        // Halton sequence generator
        private static float HaltonSequence(int index, int baseNum)
        {
            float result = 0.0f;
            float f = 1.0f;
            int i = index;
            while (i > 0)
            {
                f /= baseNum;
                result += f * (i % baseNum);
                i /= baseNum;
            }
            return result;
        }

        // Generate 2D Halton sample
        private static float2 Halton2D(int index)
        {
            return new float2(HaltonSequence(index, 2), HaltonSequence(index, 3));
        }

        // Importance sample GGX distribution for specular reflections, alpha = roughness * roughness
        private static float3 ImportanceSampleGGX(float2 u, float3 normal, float alpha)
        {
            float phi = 2f * PI * u.X;
            float cosTheta = Hlsl.Sqrt((1f - u.Y) / (1f + (alpha * alpha - 1f) * u.Y));
            float sinTheta = Hlsl.Sqrt(1f - cosTheta * cosTheta);

            // Spherical to cartesian
            float3 h = new float3(Hlsl.Cos(phi) * sinTheta, Hlsl.Sin(phi) * sinTheta, cosTheta);

            // Tangent space to world space
            float3 up = Hlsl.Abs(normal.Z) < 0.999f ? new float3(0, 0, 1) : new float3(1, 0, 0);
            float3 tangent = Hlsl.Normalize(Hlsl.Cross(up, normal));
            float3 bitangent = Hlsl.Cross(normal, tangent);
            return Hlsl.Normalize(tangent * h.X + bitangent * h.Y + normal * h.Z);
        }

        private float3 DebugBRDF(int2 pixel, float3 rayDir)
        {
            float3 outputVec = float3.Zero;
            uint2 idData = entityIdBuffer[pixel.X + pixel.Y * (int)width];
            if (idData.X != uint.MaxValue)
            {
                SDFObjectDTO entity = sdfObjects[(int)idData.X];
                float3 normal = depthNormals[pixel].GBA;
                float3 viewDir = -rayDir;

                if (Hlsl.Length(-worldData.mainLightDir) > 0f)
                {
                    float NoL = Hlsl.Max(Hlsl.Dot(normal, -worldData.mainLightDir), 0f);
                    if (NoL > 0f)
                    {
                        // Visualize different BRDF components
                        float3 brdf = BRDFMicrofacetFunction(-worldData.mainLightDir, viewDir, normal,
                            entity.metallic, entity.roughness * entity.roughness,
                            entity.color.RGB, entity.specular);

                        if (debugMode == 6) // Full BRDF
                            outputVec = brdf;
                        else if (debugMode == 7) // Specular only
                        {
                            // Calculate just the specular component
                            float3 halfwayDir = Hlsl.Normalize(viewDir + -worldData.mainLightDir);
                            float NoV = Hlsl.Saturate(Hlsl.Dot(normal, viewDir));
                            float NoH = Hlsl.Saturate(Hlsl.Dot(normal, halfwayDir));
                            float VoH = Hlsl.Saturate(Hlsl.Dot(viewDir, halfwayDir));

                            float3 f0 = float3.One * 0.16f * entity.specular * entity.specular;
                            f0 = Hlsl.Lerp(f0, entity.color.RGB, new float3(entity.metallic, entity.metallic, entity.metallic));

                            float3 F = FresnelSchlick(VoH, f0);
                            float D = D_GGX(NoH, entity.roughness * entity.roughness);
                            float G = GSmith(NoV, NoL, entity.roughness * entity.roughness);
                            float denominator = 4f * Hlsl.Max(NoV * NoL, EPSILON);
                            float3 specular = F * D * G / denominator;

                            outputVec = specular;
                        }
                        else if (debugMode == 8) // Diffuse only
                        {
                            // Calculate just the diffuse component
                            float NoV = Hlsl.Saturate(Hlsl.Dot(normal, viewDir));
                            float VoH = Hlsl.Saturate(Hlsl.Dot(viewDir, Hlsl.Normalize(viewDir + -worldData.mainLightDir)));

                            float3 rhoD = entity.color.RGB;
                            rhoD *= DisneyDiffuseFactor(NoV, NoL, VoH, entity.roughness * entity.roughness);
                            float3 diff = rhoD * RECIPROCAL_PI;

                            outputVec = diff;
                        }
                    }
                }
            }

            return outputVec;
            //float3 H = Hlsl.Normalize(V + L);
            //float NdotV = Hlsl.Max(Hlsl.Dot(N, V), 0.0f);
            //float NdotL = Hlsl.Max(Hlsl.Dot(N, L), 0.0f);
            //float NdotH = Hlsl.Max(Hlsl.Dot(N, H), 0.0f);

            //float D = D_GGX(NdotH, roughness);
            //float G = G1_GGX_Schlick(NdotV, roughness) * G1_GGX_Schlick(NdotL, roughness);

            //float3 f0 = float3.One * 0.16f * reflectance * reflectance;
            //float3 F = FresnelSchlick(Hlsl.Max(Hlsl.Dot(V, H), 0.0f), f0);

            //// Return RGB with R = D, G = G, B = average(F)
            //return new float3(D, G, (F.X + F.Y + F.Z) / 3.0f);
        }

        // --------------
        // Volumetric Fog
        // --------------

        // Henyey-Greenstein phase function for volumetric scattering
        private float PhaseFunction(float cosTheta, float g)
        {
            float g2 = g * g;
            return (1.0f - g2) / (4.0f * PI * Hlsl.Pow(1.0f + g2 - 2.0f * g * cosTheta, 1.5f));
        }

        // Simple volumetric fog integration along ray
        private float3 IntegrateVolumetricFog(float3 rayOrigin, float3 rayDir, float startDist, float endDist,
            float3 lightDir, float3 lightColor)
        {
            if (worldData.fogDensity <= 0.0f) return float3.Zero;

            float3 fogColor = float3.Zero;
            float stepSize = (endDist - startDist) / 16.0f; // Simple fixed steps for performance
            float t = startDist;

            for (int i = 0; i < 16; i++)
            {
                float3 samplePoint = rayOrigin + rayDir * t;

                // Sample fog density (could be modulated by SDF if needed)
                float density = worldData.fogDensity;

                // Calculate in-scattering from light
                float3 inScatter = float3.Zero;
                if (worldData.fogScattering > 0.0f)
                {
                    // Shadow test for light in fog (optional for performance)
                    float3 lightVec = lightDir;
                    float NoL = Hlsl.Max(Hlsl.Dot(rayDir, lightDir), 0.0f);

                    // Henyey-Greenstein phase function
                    float phase = PhaseFunction(NoL, worldData.fogAnisotropy);

                    // Light contribution
                    inScatter = lightColor * worldData.fogScattering * phase * density;
                }

                // Accumulate fog (simplified exponential absorption)
                float transmittance = Hlsl.Exp(-worldData.fogAbsorption * density * stepSize);
                fogColor += (worldData.fogColor.RGB * density * stepSize + inScatter * stepSize) * transmittance;

                t += stepSize;
                if (t >= endDist) break;
            }

            return fogColor;
        }

        // -----------
        // Raymarching
        // -----------

        private float3 Raymarch(float3 rayOrigin, float3 rayDir, int maxSteps, float farClipPlane, out int closestObj, out float depth, out int steps)
        {
            depth = worldData.nearPlane;
            closestObj = -1;
            float3 hitPoint = rayOrigin;

            for (steps = 0; steps < maxSteps && depth < farClipPlane; steps++)
            {
                hitPoint = rayOrigin + rayDir * depth;
                float worldDist = WorldSDF(hitPoint, false, uint.MaxValue, out closestObj);

                if (worldDist < AdaptiveEpsilon(depth)) break;
                depth += worldDist;
            }
            return hitPoint;
        }

        private static bool Refract(float3 incident, float3 normal, float eta, out float3 refracted)
        {
            float NdotI = Hlsl.Dot(normal, incident);
            float k = 1.0f - eta * eta * (1.0f - NdotI * NdotI);
            if (k < 0f)
            {
                refracted = float3.Zero;
                return false; // Total internal reflection
            }
            refracted = eta * incident - (eta * NdotI + Hlsl.Sqrt(k)) * normal;
            refracted = Hlsl.Normalize(refracted);
            return true;
        }

        private float3 TraceRefractionRay(float3 startDir, float3 startOrigin, float3 ambientBase, float3 normal, SDFObjectDTO startMat, out int steps)
        {
            float3 totalTransmittance = float3.One; // Start with full transmittance
            float3 accumulatedColor = float3.Zero;

            // Current state
            float3 currentOrigin = startOrigin;
            float3 currentDir = startDir;
            float3 currentNormal = normal;
            SDFObjectDTO currentMat = startMat;
            bool currentlyInsideObject = true;
            steps = 0;

            for (int transmit = 0; transmit < startMat.refractMaxRecursion; transmit++)
            {
                // Determine direction (into or out of material)
                float currentEta = currentlyInsideObject ? 1.0f / currentMat.ior : currentMat.ior;
                if (Refract(currentDir, currentNormal, currentEta, out float3 refractDir))
                {
                    // Trace through the current medium
                    float3 entryPt = currentOrigin - currentNormal * EPSILON * (currentlyInsideObject ? 1f : -1f);
                    float3 p = entryPt;
                    float travelDistance = 0f;
                    bool foundExit = false;
                    float3 exitPt = float3.Zero;
                    float3 exitNorm = float3.Zero;
                    float3 absorptionCoefficient = Hlsl.Log(Hlsl.Max(currentMat.absorptionColor.RGB, 0.001f));

                    for (int i = 0; i < currentMat.refractionMaxSteps; i++)
                    {
                        float d = WorldSDF(p, false, uint.MaxValue, out int closestObj);

                        // Check crossed boundary
                        bool nowInside = d < 0f;
                        if (currentlyInsideObject != nowInside)
                        {
                            exitPt = p;
                            exitNorm = FastNormal(p);
                            int exitClosestObj = closestObj;
                            if (Hlsl.Dot(exitNorm, refractDir) > 0) exitNorm = -exitNorm;
                            foundExit = true;

                            // Calculate transmittance
                            float3 segmentTransmittance = Hlsl.Exp(absorptionCoefficient * travelDistance * currentMat.absorptionColor.A * 5f);
                            totalTransmittance *= segmentTransmittance;

                            // Update state
                            currentlyInsideObject = nowInside;
                            if (currentlyInsideObject && exitClosestObj != -1)
                            {
                                currentMat = sdfObjects[exitClosestObj];
                                absorptionCoefficient = Hlsl.Log(Hlsl.Max(currentMat.absorptionColor.RGB, 0.001f));
                            }
                            break;
                        }

                        // Still in same medium
                        float stepSize = Hlsl.Max(Hlsl.Abs(d), AdaptiveEpsilon(travelDistance));
                        p += refractDir * stepSize;
                        travelDistance += stepSize;
                    }

                    int tracedSteps;
                    if (!foundExit)
                    {
                        // Apply final absorption
                        float3 segmentTransmittance = Hlsl.Exp(absorptionCoefficient * travelDistance * currentMat.absorptionColor.A * 5f);
                        totalTransmittance *= segmentTransmittance;

                        float3 bgColor = TraceRefractionExitRay(p, refractDir, ambientBase, out _, out _, out tracedSteps);
                        accumulatedColor = bgColor;
                        steps += tracedSteps;
                        break;
                    }

                    // Found exit
                    currentOrigin = exitPt;
                    currentDir = refractDir;
                    currentNormal = exitNorm;

                    if (!currentlyInsideObject) // If just exited to air
                    {
                        // Raymarch from exit point
                        float3 rayStart = exitPt + currentNormal * EPSILON;
                        float3 hitPoint = Raymarch(rayStart, currentDir, worldData.maxRaySteps, worldData.farPlane,
                            out int nextObjIndex, out _, out tracedSteps);
                        steps += tracedSteps;

                        if (nextObjIndex >= 0 && nextObjIndex < sdfObjects.Length)
                        {
                            currentOrigin = hitPoint;
                            currentNormal = FastNormal(hitPoint);
                            if (Hlsl.Dot(currentNormal, currentDir) > 0) currentNormal = -currentNormal;
                            currentMat = sdfObjects[nextObjIndex];
                            currentlyInsideObject = true;
                        }
                        else
                        {
                            float3 bgColor = TraceRefractionExitRay(rayStart, currentDir, ambientBase, out _, out _, out tracedSteps);
                            accumulatedColor = bgColor;
                            steps += tracedSteps;
                            break;
                        }
                    }
                }
                else break; // Total internal reflection
            }

            return accumulatedColor * totalTransmittance;
        }

        private float3 TraceRefractionExitRay(float3 rayOrigin, float3 rayDir, float3 ambientBase,
            out float3 normal, out float totalDist, out int steps)
        {
            float3 finalColor = float3.Zero;
            normal = float3.Zero;

            // Trace
            int maxRaySteps = worldData.maxRaySteps;
            float3 hitPoint = Raymarch(rayOrigin, rayDir, maxRaySteps, worldData.farPlane, out int closestObjIndex, out totalDist, out steps);
            if (closestObjIndex == -1 || totalDist > worldData.farPlane)
            {
                finalColor += worldData.backgroundColor.XYZ;
                return finalColor;
            }

            // Hit surface
            normal = FastNormal(hitPoint);
            float3 viewDir = -rayDir;

            // Calculate ambient occlusion for the hit point
            SDFObjectDTO entity = sdfObjects[closestObjIndex];
            float aoValue = 1f;
            if (entity.aoValues.X > 0f)
                aoValue = Hlsl.Lerp(1f, CalculateAO(hitPoint, normal, entity), entity.aoValues.X);

            // Calculate lighting for refraction
            float3 directLight = CalculateLighting(hitPoint, normal, viewDir, entity);
            float3 ambientLight = ambientBase * aoValue;
            finalColor += ambientLight + directLight;
            return finalColor;
        }

        /// <summary>
        /// Actually performs the main raymarching calculations.
        /// </summary>
        /// <param name="rayOrigin">Ray origin to start at</param>
        /// <param name="rayDir">Ray direction to travel</param>
        /// <param name="outputNormal">Outputs surface normal</param>
        /// <param name="totalDist">Total distance traversed</param> 
        /// <returns>Output raymarch color, with effects</returns>
        private float3 TraceRay(int2 pixel, float3 rayOrigin, float3 rayDir, int sampleIndexInPixel,
            out float3 outputNormal, out float totalDist, out int steps)
        {
            float3 finalColor = float3.Zero;
            float3 contribution = float3.One;
            float3 ambientBase = float3.Zero;
            float cosTheta = 0f;

            float3 refractedLight = float3.Zero;
            SDFObjectDTO mainMat = default;
            outputNormal = float3.Zero;
            totalDist = 0f;
            float farClipPlane = worldData.farPlane;
            bool firstHit = true;
            steps = 0;

            // Refraction coloring
            float3 surfaceColor = float3.Zero;
            float fresnelFactor = 0f;
            bool isRefractive = false;
            float aoValue = 1f;

            // Adaptive reflection step sizes
            int maxRaySteps = worldData.maxRaySteps;
            for (int bounce = 0; bounce < 32; bounce++)
            {
                // Raymarch
                float3 hitPoint = Raymarch(rayOrigin, rayDir, maxRaySteps, farClipPlane,
                    out int closestObjIndex, out float depth, out int tracedSteps);
                steps += tracedSteps;

                if (closestObjIndex == -1 || depth > farClipPlane)
                {
                    finalColor += contribution * worldData.backgroundColor.XYZ;
                    if (firstHit) totalDist = depth;
                    break;
                }

                // Hit surface
                float3 normal = FastNormal(hitPoint);
                if (Hlsl.Dot(normal, rayDir) > 0f)
                    normal = -normal;
                float3 viewDir = -rayDir;

                // Get material properties
                SDFObjectDTO entity = sdfObjects[closestObjIndex];
                float metallic = entity.metallic;
                float roughness = Hlsl.Max(entity.roughness, 0.1f);
                float aoStrength = entity.aoValues.X;

                // Calculate fresnel term
                cosTheta = Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f);
                if (firstHit)
                {
                    outputNormal = normal;
                    totalDist = depth;
                    entityIdBuffer[pixel.X + pixel.Y * (int)width] = new uint2((uint)closestObjIndex, entity.entityId);
                    mainMat = entity;

                    // Calculate ambient light
                    ambientBase = Hlsl.Lerp(entity.color.RGB, worldData.backgroundColor.RGB, worldData.ambientStrength) * worldData.ambientStrength;
                    if (aoStrength > 0f)
                        aoValue = Hlsl.Lerp(1f, CalculateAO(hitPoint, normal, entity), aoStrength);

                    if (entity.hasRefraction == 1)
                    {
                        isRefractive = true;
                        fresnelFactor = SimpleFresnelDielectric(cosTheta, mainMat.f0_dielectric);
                        refractedLight = TraceRefractionRay(rayDir, hitPoint, ambientBase, normal, entity, out tracedSteps);
                        steps += tracedSteps;
                    }
                    firstHit = false;
                }

                // Lighting with ambient occlusion
                float3 directLight = CalculateLighting(hitPoint, normal, viewDir, entity);
                if (Hlsl.Length(directLight) > 0.001f) aoValue = 1f;
                float3 ambientLight = ambientBase * aoValue;

                if (bounce == 0) surfaceColor = directLight;
                finalColor += contribution * (ambientLight + directLight);

                // Reflections
                if (entity.hasReflection == 0) break;
                if (bounce == entity.reflectionMaxBounces - 1) break;

                maxRaySteps = (int)(maxRaySteps / entity.reflectRayStepFalloff);
                float3 F = FresnelSchlick(Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f), entity.f0_reflectance);
                float reflectionChance = Hlsl.Lerp(F.X, 1f, metallic);

                if (bounce == 0 && isRefractive) reflectionChance = fresnelFactor;
                if (reflectionChance < MIN_REFLECTION_CHANCE) break;

                contribution *= F * (1f - roughness * 0.5f);
                contribution = Hlsl.Min(contribution, 10f);
                if (contribution.X + contribution.Y + contribution.Z < MIN_THROUGHPUT) break;

                int reflectionSampleIndex = pixel.X * 73 + pixel.Y * 9277 + frameCount * 1973 + sampleIndexInPixel * 3271 + bounce * 997;
                float2 u = Halton2D(reflectionSampleIndex);
                float3 halfVector = ImportanceSampleGGX(u, normal, roughness * roughness);
                float3 reflectDir = Hlsl.Normalize(Hlsl.Reflect(rayDir, halfVector));
                if (Hlsl.Dot(reflectDir, normal) < 0.01f) break;

                rayOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS;
                rayDir = reflectDir;
            }

            float3 outputColor;
            if (isRefractive)
            {
                float fresnel = Hlsl.Lerp(0.04f, 1.0f, Hlsl.Pow(1.0f - cosTheta, mainMat.reflectance));
                float3 reflectiveCol = finalColor + surfaceColor;
                outputColor = reflectiveCol * fresnel + refractedLight * (1f - fresnel);
            }
            else outputColor = finalColor + surfaceColor;

            return outputColor;
        }

        /// <summary>
        /// Executes the raymarching sequence.
        /// </summary>
        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            texture[pixel] = new float4(0, 0, 0, 0);
            depthNormals[pixel] = new float4(0, 0, 0, 0);
            entityIdBuffer[pixel.X + pixel.Y * (int)width] = new uint2(uint.MaxValue, uint.MaxValue);

            float2 uv = (float2)pixel / new float2(width, height) * 2f - 1f; // UV math
            float3 rayOrigin = worldData.cameraOrigin;

            float3 accumulatedColor = float3.Zero;
            float3 accumulatedNormal = float3.Zero;
            float3 rayDir = float3.Zero;
            float accumulatedDistance = 0f;
            int steps = 0;

            for (int sample = 0; sample < SAMPLES_PER_PIXEL; sample++)
            {
                // Add slight jitter using Halton for antialiasing
                if (SAMPLES_PER_PIXEL > 1)
                {
                    int cameraSampleIndex = pixel.X * 73 + pixel.Y * 9277 + frameCount * 1973 + sample;
                    float2 jitter = (Halton2D(cameraSampleIndex) - 0.5f) / new float2(width, height);
                    float2 jitteredUV = uv + jitter * 2f;
                    rayDir = GetCameraRayDirNew(jitteredUV);
                }
                //else
                //{
                //    rayDir = GetCameraRayDirNew(uv);
                //}

                // Automatically skip reflection bounces for non-reflective materials
                float3 color = TraceRay(pixel, rayOrigin, rayDir, sample,
                    out float3 outputNormal, out float dist, out int tracedSteps);
                steps += tracedSteps;
                accumulatedColor += color;
                accumulatedNormal += outputNormal;
                accumulatedDistance += dist;
            }

            float maxPossibleDistance = worldData.farPlane - worldData.nearPlane;
            float3 finalColor = accumulatedColor / SAMPLES_PER_PIXEL;
            float3 finalNormal = accumulatedNormal / SAMPLES_PER_PIXEL;
            float finalDist = accumulatedDistance / SAMPLES_PER_PIXEL;

            // Optional ACES:
            finalColor = Hlsl.Saturate(finalColor * (2.51f * finalColor + 0.03f) / (finalColor * (2.43f * finalColor + 0.59f) + 0.14f));

            // Final
            texture[pixel] = new float4(finalColor, 1f);
            depthNormals[pixel] = new float4(finalDist / maxPossibleDistance, finalNormal);

            // Debugging:
            if (debugMode > 0)
            {
                if (debugMode == 1) // Depth buffer
                {
                    float depth = depthNormals[pixel].R;
                    texture[pixel] = new float4(depth, depth, depth, 1);
                }
                else if (debugMode == 2) // World normal buffer
                    texture[pixel] = new float4(depthNormals[pixel].GBA, 1);
                else if (debugMode == 3) // Object ID buffer
                {
                    float3 objColor = IntToColor(entityIdBuffer[pixel.X + pixel.Y * (int)width].X);
                    texture[pixel] = new float4(objColor, 1);
                }
                else if (debugMode == 4) // Ray steps
                {
                    float stepValue1 = Hlsl.Saturate((float)steps / worldData.maxRaySteps);
                    float stepValue2 = Hlsl.Saturate((float)(steps - worldData.maxRaySteps) / worldData.maxRaySteps);
                    float stepValue3 = Hlsl.Saturate((float)(steps - worldData.maxRaySteps * 2) / worldData.maxRaySteps);
                    texture[pixel] = new float4(stepValue1, stepValue2, stepValue3, 1);
                }
                else if (debugMode == 5) // Shadows
                {
                    // Compute shadow for the main light
                    float3 shadowColor = new float3(1, 0, 0);

                    // Re-run a simple shadow ray from the first hit point
                    uint2 idData = entityIdBuffer[pixel.X + pixel.Y * (int)width];
                    if (idData.X != uint.MaxValue)
                    {
                        SDFObjectDTO entity = sdfObjects[(int)idData.X];
                        float3 hitPoint = rayOrigin + rayDir * depthNormals[pixel].R * (worldData.farPlane - worldData.nearPlane);
                        float3 normal = depthNormals[pixel].GBA;

                        if (Hlsl.Length(worldData.mainLightDir) > 0f)
                        {
                            float3 shadowOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS;
                            float shadow = SoftShadow2(shadowOrigin, worldData.mainLightDir,
                                entity.shadowDistances.X, entity.shadowDistances.Y, out int shadowObj);
                            float3 entityColor = IntToColor(entityIdBuffer[shadowObj].X);
                            shadowColor = new float3(shadow, entityColor.G, entityColor.B);
                        }
                    }
                    texture[pixel] = new float4(shadowColor, 1);
                }
                else if (debugMode == 6 || debugMode == 7 || debugMode == 8) // BRDF, Specular, Diffuse
                {
                    float3 objColor = DebugBRDF(pixel, rayDir);
                    texture[pixel] = new float4(objColor, 1);
                }
            }
        }
    }
}

// ----------------------------
// Functions and code obseleted
// ----------------------------

/* Calculates shadows
// Adapted: https://www.shadertoy.com/view/lsKcDD
private float SoftShadow(float3 rayOrigin, float3 rayDir, float minDist, float maxDist)
{
    float res = 1.0f;
    float rayDist = minDist;

    for (int i = 0; i < 100 && rayDist < maxDist; i++)
    {
        float sceneSDF = WorldSDF(rayOrigin + rayDist * rayDir, true).X;
        res = Hlsl.Min(res, sceneSDF / (0.5f * rayDist));
        rayDist += Hlsl.Clamp(sceneSDF, 0.005f, 0.05f);

        if (res < -1.0f || rayDist > maxDist)
            break;
    }

    res = Hlsl.Max(res, -1.0f);
    return 0.25f * (1.0f + res) * (1.0f + res) * (2.0f - res);
}*/

/*private float2 SoftShadowCambridge(float3 lightPos, float3 hitPoint, float renderDepth)
{
    float3 lightDir = Hlsl.Normalize(lightPos - hitPoint);
    float kd = 1f;
    float lastObj = -1;
    int step = 0;
    for (float t = 0.1f; t < Hlsl.Length(lightPos - hitPoint) && step < renderDepth && kd > 0.001f; )
    {
        float2 worldSDF = WorldSDF(hitPoint + t * lightDir, true);
        lastObj = worldSDF.Y;
        float d = Hlsl.Abs(worldSDF.X);
        if (d < 0.001f)
        {
            kd = 0;
        }
        else
        {
            kd = Hlsl.Min(kd, 16 * d / t);
        }
        t += d;
        step++;
    }
    return new float2(kd, lastObj);
}*/

/*private float3 RIS_SampleReflection(
    int2 pixel,
    float3 hitPoint,
    float3 normal,
    float3 viewDir,
    float roughness,
    float metallic,
    float3 f0,
    int frameCount,
    int bounce,
    out float misWeight)
{
    // Reservoir for RIS
    Reservoir reservoir = new Reservoir
    {
        sumWeights = 0f,
        M = 0,
        sampleDirection = float3.Zero,
        sourcePDF = 0f,
        targetPDF = 0f
    };

    const int M_CANDIDATES = 32;  // Generate 32 candidates
    float alpha = roughness * roughness;

    for (int i = 0; i < M_CANDIDATES; i++)
    {
        // Get unique seed for this candidate
        uint seed = GetSeed(pixel, i, bounce, frameCount);

        // Generate candidate using GGX importance sampling
        float2 u = Halton2DScrambled(i, seed);
        float3 candidateDir = ImportanceSampleGGX(u, normal, roughness);

        // Ensure candidate is above surface
        float NdotL = Hlsl.Max(Hlsl.Dot(normal, candidateDir), 0f);
        if (NdotL < 0.001f) continue;

        // Evaluate source PDF (BRDF PDF)
        float3 H = Hlsl.Normalize(viewDir + candidateDir);
        float NoH = Hlsl.Max(Hlsl.Dot(normal, H), 0f);
        float VoH = Hlsl.Max(Hlsl.Dot(viewDir, H), 0f);

        // GGX PDF
        float D = D_GGX(NoH, roughness);
        float sourcePDF = D * NoH / (4.0f * VoH);

        if (sourcePDF < 1e-6f) continue;

        // Estimate incoming radiance for target PDF
        // Simple approximation: could be improved with radiance cache
        float estimatedRadiance = 1.0f;  // Placeholder - you'll improve this

        // For now, use BRDF value as target PDF
        float3 F = FresnelSchlick(VoH, f0);
        float G = GSmith(Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f),
                        NdotL, roughness);

        float3 brdfValue = F * D * G / (4.0f * NdotL * Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f));
        float targetPDF = Hlsl.Length(brdfValue) * estimatedRadiance * NdotL;

        // Get random for reservoir update
        float random = ScrambledHalton(i, 5, seed) % 1.0f;

        // Update reservoir
        reservoir = UpdateReservoir(reservoir, candidateDir, sourcePDF, targetPDF, random);
    }

    // Calculate MIS weight
    float misWeight = 1.0f;
    if (reservoir.M > 0 && reservoir.sumWeights > 0f && reservoir.sourcePDF > 0f)
    {
        misWeight = reservoir.targetPDF / (reservoir.sourcePDF * reservoir.sumWeights / reservoir.M);
    }

    return (reservoir.sampleDirection, float3.One, misWeight);
}*/

// Old lighting
//float3 ambientLightAmt = ambientBase * ao;
//float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
//float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal, metallic, roughness * roughness, albedoColor, specular);
//float3 directLight = Hlsl.Lerp(ambientLightAmt, brdf, /*shadowValue * */NoL);

// Old direct lighting
//float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
//float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal, metallic, roughness * roughness, albedoColor, specular);
//float3 directLight = Hlsl.Lerp(ambientBase * ao, brdf, shadowValue * NoL);

#pragma warning restore CA1416 // Validate platform compatibility