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
        int outputMode,
        int frameCount,
        ReadWriteTexture2D<float4> texture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadWriteTexture2D<int> bounceCountTexture,  // NEW: Output bounce counts
        ReadWriteBuffer<int> objectIdBuffer,
        ReadOnlyBuffer<SDFWorldDTO> worldData,
        ReadOnlyBuffer<SDFPrimitiveObjectDTO> sdfPrimitives) : IComputeShader
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
        const float REFLECTION_BIAS = 10f; // Multiplier for normal offset

        // Refraction constants
        const float MIN_REFRACTION_CHANCE = 0.01f;
        const float REFRACTION_BIAS = 5f; // Smaller bias for refraction

        // Lighting constants
        readonly float3 sunDir = new float3(0.5f, 0.8f, 0.3f);

        // Obtained via Deepseek: https://chat.deepseek.com/share/avavmqykeivckbnakl
        private float3 GetCameraRayDir(float2 uv)
        {
            float4 rayClip = new float4(uv, 0.0f, 1.0f);
            float4 rayView = Hlsl.Mul(worldData[0].cameraInverseProj, rayClip);
            rayView = new float4(rayView.XY, -1.0f, 0.0f);
            float3 rayWorld = Hlsl.Mul(worldData[0].cameraToWorld, rayView).XYZ;
            return Hlsl.Normalize(rayWorld);
        }
        
        // Quaternion ref: https://gist.github.com/mattatz/40a91588d5fb38240403f198a938a593
        // Quaternion multiplication
        private float4 Qmul(float4 q1, float4 q2)
        {
            return new float4(
                q2.XYZ * q1.W + q1.XYZ * q2.W + Hlsl.Cross(q1.XYZ, q2.XYZ),
                q1.W * q2.W - Hlsl.Dot(q1.XYZ, q2.XYZ)
            );
        }

        // Quaternion rotation
        private float3 RotateVector(float3 v, float4 r)
        {
            float4 r_c = r * new float4(-1, -1, -1, 1);
            return Qmul(r, Qmul(new float4(v, 0), r_c)).XYZ;
        }

        private float SphereSDF(float3 pt, float r)
        {
            //float3 q = pt - 8f * Hlsl.Round(pt / 8f);
            return Hlsl.Length(pt) - r;
        }

        private float BoxSDF(float3 pt, float3 size)
        {
            float3 q = Hlsl.Abs(pt) - size;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f);
        }

        private float RoundedBoxSDF(float3 pt, float3 size, float r)
        {
            float3 q = Hlsl.Abs(pt) - size + r;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f) - r;
        }

        private float TorusSDF(float3 pt, float2 tr)
        {
            float2 q = new float2(Hlsl.Length(pt.XZ) - tr.X, pt.Y);
            return Hlsl.Length(q) - tr.Y;
        }

        private float PyramidSDF(float3 pt, float h)
        {
            float m2 = h * h + 0.25f;

            pt.XZ = Hlsl.Abs(pt.XZ);
            pt.XZ = (pt.Z > pt.X) ? pt.ZX : pt.XZ;
            pt.XZ -= 0.5f;

            float3 q = new float3(pt.Z, h * pt.Y - 0.5f * pt.X, h * pt.X + 0.5f * pt.Y);

            float s = Hlsl.Max(-q.X, 0.0f);
            float t = Hlsl.Clamp((q.Y - 0.5f * pt.Z) / (m2 + 0.25f), 0.0f, 1.0f);

            float a = m2 * (q.X + s) * (q.X + s) + q.Y * q.Y;
            float b = m2 * (q.X + 0.5f * t) * (q.X + 0.5f * t) + (q.Y - m2 * t) * (q.Y - m2 * t);

            float d2 = Hlsl.Min(q.Y, -q.X * m2 - q.Y * 0.5f) > 0.0f ? 0.0f : Hlsl.Min(a, b);

            return Hlsl.Sqrt((d2 + q.Z * q.Z) / m2) * Hlsl.Sign(Hlsl.Max(q.Z, -pt.Y));
        }

        private float PlaneSDF(float3 pt, float3 n, float h)
        {
            return Hlsl.Dot(pt, Hlsl.Normalize(n)) + h;
        }

        // Bound not exact, for performance
        private float ConeSDF(float3 pt, float2 c, float h)
        {
            float q = Hlsl.Length(pt.XZ);
            return Hlsl.Max(Hlsl.Dot(c.XY, new float2(q, pt.Y)), -h - pt.Y);
        }

        // Vertical version, for performance
        private float CylinderSDF(float3 pt, float r, float h)
        {
            float2 d = Hlsl.Abs(new float2(Hlsl.Length(pt.XZ), pt.Y)) - new float2(r, h);
            return Hlsl.Min(Hlsl.Max(d.X, d.Y), 0.0f) + Hlsl.Length(Hlsl.Max(d, 0.0f));
        }

        // Vertical version, for performance
        private float CapsuleSDF(float3 pt, float r, float h)
        {
            pt.Y -= Hlsl.Clamp(pt.Y, 0.0f, h);
            return Hlsl.Length(pt) - r;
        }

        /// <summary>
        /// Calculates the SDF distance for the world at a point.
        /// </summary>
        /// <param name="point">World position to evaluate</param>
        /// <param name="shadowCastCheck">Should the tracer verify shadow casters</param>
        /// <returns>Float2 representing the min distance, and closest object</returns>
        private float2 WorldSDF(float3 point, bool shadowCastCheck)
        {
            float minDist = MIN_TRAVERSE_DIST;

            int closest = -1;
            for (int i = 0; i < sdfPrimitives.Length; i++)
            {
                SDFPrimitiveObjectDTO curPrimitive = sdfPrimitives[i];
                if (shadowCastCheck && !curPrimitive.shadowEffects.X) continue;
                float3 scaling = curPrimitive.scaling;
                float3 curPoint = point - curPrimitive.position; // Transform SDF
                curPoint = RotateVector(curPoint, curPrimitive.rotation); // Rotate SDF
                curPoint *= scaling;

                float dist;
                if (curPrimitive.type == 0) // Adds sphere SDFs
                    dist = SphereSDF(curPoint, curPrimitive.parameters.X);
                else if (curPrimitive.type == 1) // Adds box SDFs
                    dist = BoxSDF(curPoint, curPrimitive.parameters.XYZ);
                else if (curPrimitive.type == 2) // Adds rounded box SDFs
                    dist = RoundedBoxSDF(curPoint, curPrimitive.parameters.XYZ, curPrimitive.parameters.W);
                else if (curPrimitive.type == 3) // Adds torus SDFs
                    dist = TorusSDF(curPoint, curPrimitive.parameters.XY);
                else if (curPrimitive.type == 4) // Adds pyramid SDFs
                    dist = PyramidSDF(curPoint, curPrimitive.parameters.X);
                else if (curPrimitive.type == 5) // Adds plane SDFs
                    dist = PlaneSDF(curPoint, curPrimitive.parameters.XYZ, curPrimitive.parameters.W);
                else if (curPrimitive.type == 6) // Adds cylinder SDFs
                    dist = CylinderSDF(curPoint, curPrimitive.parameters.X, curPrimitive.parameters.Y);
                else if (curPrimitive.type == 7) // Adds capsule SDFs
                    dist = CapsuleSDF(curPoint, curPrimitive.parameters.X, curPrimitive.parameters.Y);
                else if (curPrimitive.type == 8) // Adds cone SDFs
                    dist = ConeSDF(curPoint, curPrimitive.parameters.XY, curPrimitive.parameters.Z);
                else // Default to sphere SDF
                    dist = SphereSDF(curPoint, curPrimitive.parameters.X);

                dist *= Hlsl.Min(scaling.X, Hlsl.Min(scaling.Y, scaling.Z));
                if (Hlsl.Abs(dist) < minDist)
                {
                    closest = i;
                    minDist = dist;
                }
            }

            // Return packaged minimum SDF distance and closest object index
            return new float2(minDist, closest);
        }

        /// <summary>
        /// Very fast high quality normal calculation.
        /// </summary>
        /// <param name="pos">Hit position</param>
        /// <returns>World normal vector</returns>
        private float3 FastNormal(float3 pos)
        {
            float3 n = new float3(0f, 0f, 0f);
            for (int i = 0; i < 4; i++)
            {
                float3 e = 0.5773f * (2f * new float3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1f);
                n += e * WorldSDF(pos + EPSILON * 50 * e, false).X;
                if (n.X + n.Y + n.Z > 100f) break;
            }
            return Hlsl.Normalize(n);
        }

        // New soft-shadow technique:
        // Reference: https://iquilezles.org/articles/rmshadows/
        // New Version: https://www.shadertoy.com/view/tscSRS
        private float SoftShadow2(float3 point, float3 dir, float start, float end, out int closesObj)
        {
            float depth = start, dist;
            float shadow = 1f;
            float closestObjF = -1;
            for (int i = 0; i < worldData[0].maxShadowRaySteps; ++i)
            {
                float2 sdf = WorldSDF(point + depth * dir, true);
                dist = sdf.X;
                closestObjF = sdf.Y;
                if (depth > end || shadow < -1f) break;

                shadow = Hlsl.Min(shadow, 40f * dist / depth);
                depth += Hlsl.Clamp(dist, 0.005f, 10f);
            }

            closesObj = (int)closestObjF;
            shadow = Hlsl.Max(shadow, -1f);
            return Hlsl.SmoothStep(-1f, 0f, shadow);
        }

        // ------------------------------
        // New Correct PBR BRDF Functions
        // ------------------------------

        private float3 DebugBRDF(float3 N, float3 V, float3 L, float roughness, float reflectance)
        {
            float3 H = Hlsl.Normalize(V + L);
            float NdotV = Hlsl.Max(Hlsl.Dot(N, V), 0.0f);
            float NdotL = Hlsl.Max(Hlsl.Dot(N, L), 0.0f);
            float NdotH = Hlsl.Max(Hlsl.Dot(N, H), 0.0f);

            float D = D_GGX(NdotH, roughness);
            float G = G1_GGX_Schlick(NdotV, roughness) * G1_GGX_Schlick(NdotL, roughness);

            float3 f0 = float3.One * 0.16f * reflectance * reflectance;
            float3 F = FresnelSchlick(Hlsl.Max(Hlsl.Dot(V, H), 0.0f), f0);

            // Return RGB with: R = D, G = G, B = average(F)
            return new float3(D, G, (F.X + F.Y + F.Z) / 3.0f);
        }

        private float3 FresnelSchlick(float cosTheta, float3 f0)
        {
            return f0 + (float3.One - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        private float D_GGX(float NoH, float roughness)
        {
            float alpha = roughness * roughness;
            float alpha2 = alpha * alpha;
            float NoH2 = NoH * NoH;
            float b = NoH2 * (alpha2 - 1f) + 1f;
            return alpha2 * RECIPROCAL_PI / (b * b);
        }

        private float GSmith(float NoV, float NoL, float roughness)
        {
            return G1_GGX_Schlick(NoL, roughness) * G1_GGX_Schlick(NoV, roughness);
        }

        private float G1_GGX_Schlick(float NoV, float roughness)
        {
            float alpha = roughness * roughness;
            float k = alpha / 2f;
            return Hlsl.Max(NoV, EPSILON) / (NoV * (1f - k) + k);
        }

        // Special Disney Rendering
        private float FresnelSchlick90(float cosTheta, float f0, float f90)
        {
            return f0 + (f90 - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        private float DisneyDiffuseFactor(float NoV, float NoL, float VoH, float roughness)
        {
            float alpha = roughness * roughness;
            float f90 = 0.5f + 2f * alpha * VoH * VoH;
            float F_In = FresnelSchlick90(NoL, 1f, f90);
            float F_Out = FresnelSchlick90(NoV, 1f, f90);
            return F_In * F_Out;
        }

        private float3 BRDFMicrofacetFunction(float3 lightDir, float3 viewDir, float3 normal, float metallic, float roughness, float3 baseCol, float reflectance)
        {
            float3 halfwayDir = Hlsl.Normalize(viewDir + lightDir);
            float NoV = Hlsl.Clamp(Hlsl.Dot(normal, viewDir), 0f, 1f);
            float NoL = Hlsl.Clamp(Hlsl.Dot(normal, lightDir), 0f, 1f);
            float VoH = Hlsl.Clamp(Hlsl.Dot(viewDir, halfwayDir), 0f, 1f);
            float NoH = Hlsl.Clamp(Hlsl.Dot(normal, halfwayDir), 0f, 1f);

            float3 f0 = float3.One * 0.16f * reflectance * reflectance;
            f0 = Hlsl.Lerp(f0, baseCol, new float3(metallic, metallic, metallic));

            float3 F = FresnelSchlick(VoH, f0);
            float D = D_GGX(NoH, roughness);
            float G = GSmith(NoV, NoL, roughness);

            // FIX 1: Add epsilon to denominator to prevent division by near-zero
            float denominator = 4f * Hlsl.Max(NoV * NoL, EPSILON);
            float3 specular = F * D * G / denominator;

            // FIX 2: Clamp specular to prevent fireflies
            specular = Hlsl.Min(specular, 20.0f); // Limit max brightness

            // Diffuse
            float3 rhoD = baseCol;
            rhoD *= DisneyDiffuseFactor(NoV, NoL, VoH, roughness);
            rhoD *= 1f - metallic;
            float3 diff = rhoD * RECIPROCAL_PI;

            // FIX 3: Clamp final BRDF result
            return Hlsl.Min(diff + specular, 100.0f); // Hard cap to prevent explosions
        }

        /// <summary>
        /// Calculates Fresnel reflectance for dielectrics (glass, water, etc.)
        /// </summary>
        private float FresnelDielectric(float cosI, float ior)
        {
            // Clamp to valid range
            cosI = Hlsl.Clamp(cosI, -1.0f, 1.0f);

            bool entering = cosI > 0.0f;
            float etaI = entering ? 1.0f : ior;
            float etaT = entering ? ior : 1.0f;

            // Snell's law
            float sinT = etaI / etaT * Hlsl.Sqrt(Hlsl.Max(0.0f, 1.0f - cosI * cosI));

            // Total internal reflection
            if (sinT >= 1.0f)
                return 1.0f;

            float cosT = Hlsl.Sqrt(Hlsl.Max(0.0f, 1.0f - sinT * sinT));
            cosI = Hlsl.Abs(cosI);

            // Fresnel equations
            float rParallel = ((etaT * cosI) - (etaI * cosT)) / ((etaT * cosI) + (etaI * cosT));
            float rPerpendicular = ((etaI * cosI) - (etaT * cosT)) / ((etaI * cosI) + (etaT * cosT));

            return (rParallel * rParallel + rPerpendicular * rPerpendicular) / 2.0f;
        }

        // Reflections functions:

        private uint HaltonHash(uint x)
        {
            x = x ^ 61 ^ (x >> 16);
            x += x << 3;
            x ^= x >> 4;
            x *= 0x27d4eb2d;
            x ^= x >> 15;
            return x;
        }

        // Halton sequence generator
        private float HaltonSequence(int index, int baseNum)
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
        private float2 Halton2D(int index)
        {
            return new float2(HaltonSequence(index, 2), HaltonSequence(index, 3));
        }

        // Importance sample GGX distribution for specular reflections
        private float3 ImportanceSampleGGX(float2 u, float3 normal, float roughness)
        {
            float alpha = roughness * roughness;
            float phi = 2.0f * PI * u.X;
            float cosTheta = Hlsl.Sqrt((1.0f - u.Y) / (1.0f + (alpha * alpha - 1.0f) * u.Y));
            float sinTheta = Hlsl.Sqrt(1.0f - cosTheta * cosTheta);

            // Spherical to cartesian
            float3 h = new float3(Hlsl.Cos(phi) * sinTheta, Hlsl.Sin(phi) * sinTheta, cosTheta);

            // Tangent space to world space
            float3 up = Hlsl.Abs(normal.Z) < 0.999f ? new float3(0, 0, 1) : new float3(1, 0, 0);
            float3 tangent = Hlsl.Normalize(Hlsl.Cross(up, normal));
            float3 bitangent = Hlsl.Cross(normal, tangent);
            return Hlsl.Normalize(tangent * h.X + bitangent * h.Y + normal * h.Z);
        }

        private float3 Raymarch(float3 rayOrigin, float3 rayDir, int maxSteps, float farClipPlane, out int closestObj, out float depth)
        {
            // Raymarch
            depth = worldData[0].nearPlane;
            closestObj = -1;
            float3 hitPoint = rayOrigin;

            for (int step = 0; step < maxSteps; step++)
            {
                hitPoint = rayOrigin + rayDir * depth;
                float2 worldSDFData = WorldSDF(hitPoint, false);
                float worldDist = worldSDFData.X;
                closestObj = (int)worldSDFData.Y;

                if (worldDist < EPSILON) break;
                depth += worldDist;
                if (depth > farClipPlane) break;
            }

            return hitPoint;
        }

        private float3 RefractRaymarch(float3 rayOrigin, float3 rayDir, int maxSteps, float farClipPlane, out float depth)
        {
            // Raymarch
            depth = worldData[0].nearPlane;
            float3 hitPoint = rayOrigin;

            for (int step = 0; step < maxSteps; step++)
            {
                hitPoint = rayOrigin + rayDir * depth;
                float2 worldSDFData = WorldSDF(hitPoint, false);
                float worldDist = worldSDFData.X;

                if (worldDist > EPSILON) break;
                depth -= worldDist;
                if (depth > farClipPlane) break;
            }
            return hitPoint;
        }

        /// <summary>
        /// Performs refraction raymarching through a solid object
        /// </summary>
        private float3 TraceRefractionRay(float3 rayDir, float ior, float3 surfaceNormal,
            float3 hitPoint, out float3 exitPoint, out float3 exitNormal)
        {
            // First refraction: from air into material
            float eta = 1.0f / ior; // air to material
            float3 refractedDir = Hlsl.Refract(rayDir, surfaceNormal, eta);

            // Check for total internal reflection
            if (Hlsl.Length(refractedDir) < EPSILON)
            {
                // Total internal reflection - just reflect instead
                exitPoint = hitPoint;
                exitNormal = surfaceNormal;
                return Hlsl.Reflect(rayDir, surfaceNormal);
            }

            // Raymarch through the material to find the back surface
            float3 currentPos = hitPoint - surfaceNormal * EPSILON * REFRACTION_BIAS; // Move inside material
            float marchDist = 0.0f;
            const float maxRefractionDepth = 100.0f;

            for (int step = 0; step < worldData[0].maxRaySteps && marchDist < maxRefractionDepth; step++)
            {
                float2 sdf = WorldSDF(currentPos, false);
                float dist = sdf.X;

                if (dist < EPSILON)
                {
                    // Found the back surface
                    exitPoint = currentPos;
                    exitNormal = -FastNormal(currentPos); // Normal points outward from material

                    // Second refraction: from material back to air
                    eta = ior; // material to air
                    float3 exitDir = Hlsl.Refract(refractedDir, -exitNormal, eta);

                    // Check for total internal reflection at back surface
                    if (Hlsl.Length(exitDir) < EPSILON)
                    {
                        // Total internal reflection at back surface
                        return Hlsl.Reflect(refractedDir, -exitNormal);
                    }

                    return exitDir;
                }

                // Move forward in refraction direction
                currentPos += refractedDir * Hlsl.Max(dist, EPSILON * 10f);
                marchDist += Hlsl.Max(dist, EPSILON * 10f);
            }

            // If we exit the loop without finding a back surface, just continue in same direction
            exitPoint = currentPos;
            exitNormal = -refractedDir; // Arbitrary exit normal
            return refractedDir;
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
            out float3 outputNormal, out float totalDist, out int actualBounces)
        {
            float3 finalColor = float3.Zero;
            float3 throughput = float3.One;
            float3 lightDir = Hlsl.Normalize(sunDir);
            outputNormal = float3.Zero;
            totalDist = 0f;
            float farClipPlane = worldData[0].farPlane;
            bool firstHit = true;
            actualBounces = 0;  // Track how many bounces actually occurred

            // Adaptive reflection step sizes
            int maxRaySteps = worldData[0].maxRaySteps;
            for (int bounce = 0; bounce < 32; bounce++) // Cap software enforced bounce limit of 32
            {
                // Raymarch
                float3 hitPoint = Raymarch(rayOrigin, rayDir, maxRaySteps, farClipPlane, out int closestObjIndex, out float depth);

                // Miss - add sky color and exit
                if (closestObjIndex == -1 || depth > farClipPlane)
                {
                    finalColor += throughput * worldData[0].backgroundColor.XYZ;
                    if (firstHit) totalDist = depth;
                    break;
                }

                // Hit surface
                float3 normal = FastNormal(hitPoint);
                float3 viewDir = -rayDir;
                actualBounces = bounce + 1; // Count this bounce

                // Store first hit data for output
                if (firstHit)
                {
                    outputNormal = normal;
                    totalDist = depth;
                    objectIdBuffer[pixel.X + pixel.Y * (int)width] = closestObjIndex;
                    firstHit = false;
                }

                // Get material
                SDFPrimitiveObjectDTO material = sdfPrimitives[closestObjIndex];
                float3 albedoColor = material.color.RGB;
                float metallic = material.metallic;
                float roughness = Hlsl.Max(material.roughness, 0.1f);
                float specular = material.specular;
                float ao = material.ao;

                // Calculate F0 for fresnel
                float3 f0 = float3.One * 0.16f * specular * specular;
                f0 = Hlsl.Lerp(f0, albedoColor, new float3(metallic, metallic, metallic));

                // Ambient lighting
                float3 ambientLightAmt = float3.One * 0.15f * worldData[0].backgroundColor.RGB * ao;

                // Shadows
                float shadowValue = 1f;
                int closestShadowObj = -1;
                if (material.shadowEffects.Y)
                {
                    float3 shadowOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS;
                    float2 shadowDistances = material.shadowDistances;
                    shadowValue = SoftShadow2(shadowOrigin, lightDir, shadowDistances.X, shadowDistances.Y, out closestShadowObj);
                }

                // Refractions
                if (material.hasRefraction == 1 && material.ior > 1.0f)
                {
                    float3 refractRayDir = Hlsl.Refract(rayDir, normal, material.ior);
                    float3 refractRayOrigin = refractRayDir * EPSILON * REFRACTION_BIAS + rayOrigin;

                    // Debug refraction rays
                    finalColor += (Hlsl.Normalize(refractRayDir - rayDir) + float3.One) / 2f;

                    float3 refractExitPoint = RefractRaymarch(rayOrigin, rayDir, material.refractionMaxSteps, farClipPlane, out float refractDepth);
                }

                // Direct lighting
                float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
                float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal, metallic, roughness, albedoColor, specular);
                float3 directLight = Hlsl.Lerp(ambientLightAmt, brdf, shadowValue * NoL);
                finalColor += throughput * directLight;

                // Reflections
                if (material.hasReflection == 0) break;
                if (bounce == material.reflectionMaxBounces - 1) break;
                maxRaySteps = (int)(maxRaySteps / material.reflectRayStepFalloff);

                float3 F = FresnelSchlick(Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f), f0);
                float reflectionChance = Hlsl.Lerp(F.X, 1f, metallic); // Calculate reflection probability
                if (reflectionChance < MIN_REFLECTION_CHANCE) break; // If very little reflection, exit
                throughput *= F * (1f - roughness * 0.5f); // Update throughput for next bounce
                throughput = Hlsl.Min(throughput, 10f); // Clamp throughput
                if (throughput.X + throughput.Y + throughput.Z < MIN_THROUGHPUT) break; // If throughput is too low, exit early

                int reflectionSampleIndex = pixel.X * 73 + pixel.Y * 9277 + frameCount * 1973 + sampleIndexInPixel * 3271 + bounce * 997;
                float2 u = Halton2D(reflectionSampleIndex); // Generate reflection ray for next bounce
                float3 halfVector = ImportanceSampleGGX(u, normal, roughness); // Importance sample based on roughness
                float3 reflectDir = Hlsl.Reflect(-viewDir, halfVector);
                if (Hlsl.Dot(reflectDir, normal) < 0.01f) break; // Make sure reflection is above surface

                rayOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS; // Prepare for next iteration
                rayDir = reflectDir;
            }

            return finalColor;
        }

        private float2 RandomInUnitCircle(uint rngState)
        {
            uint rngHash = HaltonHash(rngState);
            float angle = rngHash * 2 * PI;
            float2 pointOnCircle = new float2(Hlsl.Cos(angle), Hlsl.Sin(angle));
            return pointOnCircle * Hlsl.Sqrt(rngHash);
        }

        /// <summary>
        /// Executes the raymarching sequence.
        /// </summary>
        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            texture[pixel] = new float4(0, 0, 0, 0);
            depthNormals[pixel] = new float4(0, 0, 0, 0);
            objectIdBuffer[pixel.X + pixel.Y * (int)width] = -1;
            bounceCountTexture[pixel] = 0;  // Initialize bounce count

            float2 uv = (float2)pixel / new float2(width, height) * 2.0f - 1.0f;
            uv.X *= width / height;
            float3 rayOrigin = worldData[0].cameraOrigin;

            float3 accumulatedColor = float3.Zero;
            float3 accumulatedNormal = float3.Zero;
            float accumulatedDistance = 0f;
            int accumulatedBounces = 0;  // NEW: Accumulate bounce counts

            for (int sample = 0; sample < SAMPLES_PER_PIXEL; sample++)
            {
                // Add slight jitter using Halton for antialiasing
                float3 rayDir;
                if (SAMPLES_PER_PIXEL > 1)
                {
                    int cameraSampleIndex = pixel.X * 73 + pixel.Y * 9277 + frameCount * 1973 + sample;
                    float2 jitter = (Halton2D(cameraSampleIndex) - 0.5f) / new float2(width, height);
                    float2 jitteredUV = uv + jitter * 2f;
                    rayDir = GetCameraRayDir(jitteredUV);
                }

                // Automatically skip reflection bounces for non-reflective materials
                float3 color = TraceRay(pixel, rayOrigin, rayDir, sample,
                    out float3 outputNormal, out float dist, out int bounceCount);

                accumulatedColor += color;
                accumulatedNormal += outputNormal;
                accumulatedDistance += dist;
                accumulatedBounces += bounceCount;  // Accumulate bounces
            }

            float maxPossibleDistance = worldData[0].farPlane - worldData[0].nearPlane;
            float3 finalColor = accumulatedColor / SAMPLES_PER_PIXEL;
            float3 finalNormal = accumulatedNormal / SAMPLES_PER_PIXEL;
            float finalDist = accumulatedDistance / SAMPLES_PER_PIXEL;

            switch (outputMode)
            {
                case 0:
                    break;
                case 1:
                    finalColor = float3.One * (finalDist / maxPossibleDistance);
                    break;
                case 2:
                    finalColor = Hlsl.Normalize((finalNormal + 1f) / 2f);
                    break;
                //case 3:
                //    finalColor = new float3(objectIdBuffer[])
                case 4:
                    break;
                default:
                    break;
            }

            // Optional ACES:
            finalColor = Hlsl.Clamp((finalColor * (2.51f * finalColor + 0.03f)) / (finalColor * (2.43f * finalColor + 0.59f) + 0.14f), 0f, 1f);
            
            texture[pixel] = new float4(finalColor, 1.0f);
            depthNormals[pixel] = new float4(finalDist / maxPossibleDistance, finalNormal);
            bounceCountTexture[pixel] = accumulatedBounces / SAMPLES_PER_PIXEL; // Write bounces to map
        }
    }
}

// ----------------------------
// Functions and code obseleted
// ----------------------------

/*private float3 GetCamRayDir(float2 coord)
{
    return Hlsl.Normalize(Hlsl.Mul(worldData[0].cameraToWorld, 
        new float4(Hlsl.Mul(worldData[0].cameraInverseProj, new float4(coord, 0.0f, 1.0f)).XYZ, 0.0f)).XYZ);
}*/

// Calculates shadows
// Adapted: https://www.shadertoy.com/view/lsKcDD
/*private float SoftShadow(float3 rayOrigin, float3 rayDir, float minDist, float maxDist)
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

// Ambient occlusion
//float aoAmt = 1f;
/*if (ao > 0.001f)
{
    float3 aoPoint = hitPoint + normal * EPSILON;
    float stepDist = 0.05f;

    aoAmt = CalculatePhysicallyBasedAO(pixel, aoPoint, normal);

    // Blend with material's AO strength
    aoAmt = Hlsl.Lerp(1f, aoAmt, ao);
    //aoAmt = Hlsl.Lerp(0f, aoAmt, 1f - Hlsl.Clamp(shadowValues.X, 0, 1));
}*/

// Depth of Field section:
/*private float2 RandomPointOnDisk(uint seed, float2 pixel)
{
    // Hash-based random (replace with your preferred method)
    float r1 = Hlsl.Frac(Hlsl.Sin(seed * 12.9898f + pixel.X * 78.233f + pixel.Y * 37.719f) * 43758.5453f);
    float r2 = Hlsl.Frac(Hlsl.Sin(seed * 4.2719f + pixel.X * 63.726f + pixel.Y * 19.357f) * 13758.5453f);

    // Map to disk (uniform distribution)
    float theta = r1 * 2.0f * 3.14159265f;
    float radius = Hlsl.Sqrt(r2);

    return new float2(Hlsl.Cos(theta), Hlsl.Sin(theta)) * radius;
}

private float3 GetCameraRayDirWithDOF(float2 uv, float3 cameraOrigin, float3 cameraForward,
                                     float3 cameraRight, float3 cameraUp, float focusDistance,
                                     float apertureSize, uint seed)
{
    // Original ray direction (your existing method)
    float3 rayDir = GetCameraRayDir(uv);

    // If no DoF, return original
    if (apertureSize < 0.001f)
        return rayDir;

    // Calculate focal point
    float3 focalPoint = cameraOrigin + rayDir * focusDistance;

    // Jitter ray origin on aperture disk
    float2 diskUV = RandomPointOnDisk(seed, uv * 1000.0f);
    float3 apertureOffset = (cameraRight * diskUV.X + cameraUp * diskUV.Y) * apertureSize;
    float3 newRayOrigin = cameraOrigin + apertureOffset;

    // New ray direction toward focal point
    return Hlsl.Normalize(focalPoint - newRayOrigin);
}

private float CalculatePhysicallyBasedAO(int2 pixel, float3 p, float3 n)
{
    // --- Configuration Parameters (consider moving to worldData) ---
    const float AO_RADIUS = 1f;  // World-space sampling radius. Tune per scene!
    const float AO_POWER = 1.5f;   // Controls contrast
    float occlusion = 0.0f;
    float weightSum = 0.0f;
    float randomSeed = Hlsl.Frac(Hlsl.Sin((float)(pixel.X * 12.9898f + pixel.Y * 78.233f + frame * 37.719f)) * 43758.5453f);
    float stepSize = AO_RADIUS / 8.0f; // Adaptive step can be better

    for (int i = 0; i < 16; i++)
    {
        float3 randDir = GetRandomHemisphereDirection(i, 16, randomSeed, n);
        float3 rayOrigin = p + n * EPSILON;
        float rayDepth = 0.0f;
        float localOcclusion = 0.0f;

        for (int j = 0; j < 8; j++)
        {
            float distanceToScene = WorldSDF(rayOrigin + randDir * rayDepth, false).X;
            if (distanceToScene < EPSILON)
            {
                localOcclusion = Hlsl.Max(localOcclusion, 1f - (rayDepth / AO_RADIUS));
                break;
            }
            rayDepth += Hlsl.Max(stepSize, distanceToScene);
            if (rayDepth >= AO_RADIUS) break;
        }

        float weight = Hlsl.Max(Hlsl.Dot(n, randDir), 0f);
        occlusion += localOcclusion * weight;
        weightSum += weight;
    }

    occlusion = weightSum > 0f ? occlusion / weightSum : 0f;
    return Hlsl.Pow(Hlsl.Saturate(1f - occlusion * AO_POWER), 1f);
}

private float3 GetRandomHemisphereDirection(int sampleIndex, int sampleCount, float randomSeed, float3 normal)
{
    // Create a random angle and height using pseudo-random sequences
    float goldenRatio = 1.61803398875f;
    float phi = 2.0f * 3.14159265f * (sampleIndex * goldenRatio + randomSeed) / sampleCount;
    float cosTheta = Hlsl.Sqrt((float)(sampleIndex + 0.5f) / sampleCount); // Cosine-weighted distribution
    float sinTheta = Hlsl.Sqrt(1.0f - cosTheta * cosTheta);

    // Create a direction in a local tangent space (Z-up)
    float3 localDir = new float3(Hlsl.Cos(phi) * sinTheta, Hlsl.Sin(phi) * sinTheta, cosTheta);

    // Align local Z-axis with the surface normal
    float3 tangent = Hlsl.Normalize(Hlsl.Cross(new float3(0.0f, 1.0f, 0.0f), normal));
    if (Hlsl.Abs(Hlsl.Dot(tangent, tangent)) < 0.001f) // Handle near-vertical normals
        tangent = Hlsl.Normalize(Hlsl.Cross(new float3(1.0f, 0.0f, 0.0f), normal));
    float3 bitangent = Hlsl.Cross(normal, tangent);

    // Transform local direction to world space
    float3 worldDir = tangent * localDir.X + bitangent * localDir.Y + normal * localDir.Z;
    return Hlsl.Normalize(worldDir);
}*/

// Apply Depth of Field if enabled
/*if (apertureSize > 0.001f)
{
    // Calculate focal point on focus plane
    float3 focalPoint = rayOrigin + rayDir * focusDistance;

    // Jitter on aperture disk (scaled by focus distance)
    float2 diskUV = RandomPointOnDisk(seed, pixelCoord);
    float effectiveAperture = apertureSize * focusDistance * 0.1f; // Matches Shadertoy 0.02 scale

    float3 apertureOffset = (cameraRight * diskUV.X + cameraUp * diskUV.Y) * effectiveAperture;
    float3 newRayOrigin = rayOrigin + apertureOffset;

    // New direction toward focal point
    rayDir = Hlsl.Normalize(focalPoint - newRayOrigin);
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

/*public void Execute()
    {
        int2 pixel = ThreadIds.XY; // Get pixel position
        texture[pixel] = new float4(0, 0, 0, 0); // Clear render texture
        depthNormals[pixel] = new float4(0, 0, 0, 0); // Clear depth and normal texture
        objectIdBuffer[pixel.X + pixel.Y * (int)width] = -1; // Clear object ID buffer

        // Get uv coord
        float2 uv = (float2)pixel / new float2(width, height) * 2.0f - 1.0f;
        uv.X *= width / height;

        // Camera basis vectors (simplified - you may need proper extraction)
        float3 cameraForward = Hlsl.Normalize(new float3(
            worldData[0].cameraToWorld.M31,  // Row 3, Column 1 = forward.x
            worldData[0].cameraToWorld.M32,  // Row 3, Column 2 = forward.y
            worldData[0].cameraToWorld.M33   // Row 3, Column 3 = forward.z
        ));

        float3 cameraRight = Hlsl.Normalize(new float3(
            worldData[0].cameraToWorld.M11,  // Row 1, Column 1 = right.x
            worldData[0].cameraToWorld.M12,  // Row 1, Column 2 = right.y
            worldData[0].cameraToWorld.M13   // Row 1, Column 3 = right.z
        ));

        float3 cameraUp = Hlsl.Normalize(new float3(
            worldData[0].cameraToWorld.M21,  // Row 2, Column 1 = up.x
            worldData[0].cameraToWorld.M22,  // Row 2, Column 2 = up.y
            worldData[0].cameraToWorld.M23   // Row 2, Column 3 = up.z
        ));

        float3 rayOrigin = worldData[0].cameraOrigin;
        float focusDistance = worldData[0].focusDistance;
        float apertureSize = worldData[0].apertureSize;
        int dofSamples = Hlsl.Max(worldData[0].dofSamples, 1);

        // Accumulate color for multiple samples
        float3 accumulatedColor = float3.Zero;
        float3 accumulatedNormal = float3.Zero;
        float accumulatedDistance = 0f;

        for (int sample = 0; sample < dofSamples; sample++)
        {
            // Unique seed per sample
            float2 pixelCoord = (float2)pixel + new float2(0.5f, 0.5f);
            //uint seed = (uint)(pixelCoord.X * 1973 + pixelCoord.Y * 9277 + sample * 26699 + (uint)frame);

            // Get original ray direction
            float3 rayDir = GetCameraRayDir(uv);

            // Trace ray
            float3 color = TraceRay(pixel, rayOrigin, rayDir, out float3 outputNormal, out float totalDist);
            accumulatedNormal += outputNormal;
            accumulatedColor += color;
            accumulatedDistance += totalDist;
        }

        float maxPossibleDistance = worldData[0].farPlane - worldData[0].nearPlane;

        // Average samples
        float3 finalColor = accumulatedColor / dofSamples;
        float3 finalNormal = accumulatedNormal / dofSamples;
        float finalDist = accumulatedDistance / dofSamples;
        texture[pixel] = new float4(finalColor, 1.0f);
        depthNormals[pixel] = new float4(finalDist / maxPossibleDistance, finalNormal);
    }*/

/*private float3 TraceRay(int2 pixel, float3 rayOrigin, float3 rayDir, out float3 outputNormal, out float totalDist)
{
    // SDF depth and normal variables
    outputNormal = new float3(0, 0, 0);

    // SDF raymarch variables
    totalDist = worldData[0].nearPlane; // Start at near clip plane
    float farClipPlane = worldData[0].farPlane;
    int closestObjIndex = -1; // Clear initial object index
    float3 outputColor = worldData[0].backgroundColor.XYZ; // Set output skybox color
    float3 hitPoint = rayOrigin;

    int maxSteps = worldData[0].maxRaySteps, step;
    for (step = 0; step < maxSteps; step++)
    {
        // Accumulate ray position
        hitPoint = rayOrigin + rayDir * totalDist;

        // Calculate SDF world dist function
        float2 worldSDFData = WorldSDF(hitPoint, false);
        float worldDist = worldSDFData.X;
        if (worldDist < EPSILON)
        {
            closestObjIndex = (int)worldSDFData.Y;
            break;
        }

        // Accumulate ray dist
        totalDist += worldDist;

        // Ray missed all SDFs
        if (totalDist > farClipPlane) break;
    }
    float stepCost = step / (float)maxSteps;

    if (closestObjIndex > -1)
    {
        // Get lighting vectors
        float3 normal = FastNormal(hitPoint);
        float3 viewDir = -rayDir;
        float3 lightDir = Hlsl.Normalize(sunDir);

        // Update data buffers
        outputNormal = normal;
        objectIdBuffer[pixel.X + pixel.Y * (int)width] = closestObjIndex;

        // Get material
        SDFPrimitiveObjectDTO material = sdfPrimitives[closestObjIndex];
        float3 albedoColor = material.color.RGB;
        float metallic = material.metallic;
        float roughness = material.roughness;
        float specular = material.specular;
        float ao = material.ao;

        // Default light values
        float3 ambientLightAmt = float3.One * 0.05f * worldData[0].backgroundColor.RGB * ao;

        // Shading
        float2 shadowValues = new float2(1f, 0f);
        float3 shadowOrigin = hitPoint + normal * EPSILON;
        float2 shadowDistances = sdfPrimitives[closestObjIndex].shadowDistances;
        if (sdfPrimitives[closestObjIndex].shadowEffects.Y)
            shadowValues = SoftShadow2(shadowOrigin, lightDir, shadowDistances.X, shadowDistances.Y);

        // Lighing
        float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), EPSILON);
        float3 directLighting = BRDFMicrofacetFunction(lightDir, viewDir, normal, metallic, roughness, albedoColor, specular);
        outputColor = Hlsl.Lerp(ambientLightAmt, directLighting * 1.5f, shadowValues.X * NoL);

        // Debug shadows
        if (outputMode == 2) outputColor = new float3(shadowValues.X, shadowValues.Y / sdfPrimitives.Length, 0f);
        else if (outputMode == 3) outputColor = DebugBRDF(normal, viewDir, lightDir, roughness, specular);
    }

    if (outputMode == 1) outputColor = new float3(stepCost, stepCost, stepCost); // Debug ray steps
    return outputColor;
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

// Add these to SDFShader3D:

/*private float HaltonSequence(int index, int baseNum, uint scramble)
{
    float result = 0.0f;
    float f = 1.0f;
    int i = index;
    while (i > 0)
    {
        f /= baseNum;
        int digit = i % baseNum;
        // Use the scramble to permute the digit
        uint hashed = HaltonHash(scramble + (uint)digit + (uint)baseNum * 123456u);
        digit = (int)(hashed % (uint)baseNum);
        result += f * digit;
        i /= baseNum;
    }
    return result;
}

// Generate 2D Halton sample
private float2 Halton2D(int index)
{
    uint scramble0 = (uint)(index * 1973);
    uint scramble1 = (uint)(index * 9277);
    return new float2(HaltonSequence(index, 2, scramble0), HaltonSequence(index, 3, scramble1));
}*/

// Improved halton:
/*private float ScrambledHalton(int index, int baseNum, int seed)
{
    float result = 0f;
    float f = 1f;
    int i = index;
    while (i > 0)
    {
        f /= baseNum;
        int digit = i % baseNum;
        // XOR scrambling with seed
        int scrambled_digit = (digit + seed) % baseNum;
        result += f * scrambled_digit;
        i = (int)Hlsl.Floor(i / baseNum);
    }
    return result;
}

// For temporal accumulation across frames
private float4 GetHaltonSample2D(int pixelX, int pixelY, int sampleIndex, int frameCount)
{
    // Key insight: You need DIFFERENT sequences for different purposes
    // Use primes to avoid correlation

    // Sequence 1: For camera jitter (antialiasing)
    float sequence1 = ScrambledHalton(sampleIndex + frameCount * 97, 2, pixelX ^ pixelY);
    float sequence2 = ScrambledHalton(sampleIndex + frameCount * 97, 3, pixelX ^ pixelY ^ 12345);

    // Sequence 2: For BRDF sampling (reflections) - MUST be different!
    float sequence3 = ScrambledHalton(sampleIndex + frameCount * 113, 5, pixelX ^ pixelY ^ 54321);
    float sequence4 = ScrambledHalton(sampleIndex + frameCount * 113, 7, pixelX ^ pixelY ^ 98765);

    return new float4(sequence1, sequence2, sequence3, sequence4);
}*/

#pragma warning restore CA1416 // Validate platform compatibility