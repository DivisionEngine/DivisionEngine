using ComputeSharp;
using DivisionEngine.Rendering;

#pragma warning disable CA1416 // Validate platform compatibility
namespace DivisionEngine
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFShader3D(
        float width,
        float height,
        int outputMode,
        int frame,
        ReadWriteTexture2D<float4> texture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadWriteBuffer<int> objectIdBuffer,
        ReadOnlyBuffer<SDFWorldDTO> worldData,
        ReadOnlyBuffer<SDFPrimitiveObjectDTO> sdfPrimitives) : IComputeShader
    {
        const float EPSILON = 0.0001f;
        const float MIN_TRAVERSE_DIST = 100000000.0f;
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

        /*private float3 GetCamRayDir(float2 coord)
        {
            return Hlsl.Normalize(Hlsl.Mul(worldData[0].cameraToWorld, 
                new float4(Hlsl.Mul(worldData[0].cameraInverseProj, new float4(coord, 0.0f, 1.0f)).XYZ, 0.0f)).XYZ);
        }*/

        
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

        // Applies translation, rotation, and scaling to a point
        

        private float SphereSDF(float3 pt, float r)
        {
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
                curPoint /= Hlsl.Max(scaling, new float3(EPSILON, EPSILON, EPSILON)); // Make sure not dividing by 0.

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

        private float3 FastNormal(float3 pos)
        {
            float3 n = new float3(0, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                float3 e = 0.5773f * (2.0f * new float3((((i + 3) >> 1) & 1), ((i >> 1) & 1), (i & 1)) - 1.0f);
                n += e * WorldSDF(pos + EPSILON * 50 * e, false).X;
                //if( n.x+n.y+n.z>100.0 ) break;
            }
            return Hlsl.Normalize(n);
        }

        // Calculates shadows
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
        }

        private float Refraction(float3 rayOrigin, float3 rayDir, float minDist, float maxDist)
        {
            float res = 1.0f;
            float rayDist = minDist;

            for (int i = 0; i < 100 && rayDist < maxDist; i++)
            {
                float2 sceneSDF = WorldSDF(rayOrigin + rayDist * rayDir, true);
                res = Hlsl.Min(res, sceneSDF.X / (0.5f * rayDist));
                rayDist += Hlsl.Clamp(sceneSDF.X, 0.005f, 0.05f);

                if (res < -1.0f || rayDist > maxDist)
                    break;
            }

            res = Hlsl.Max(res, -1.0f);
            return 0.25f * (1.0f + res) * (1.0f + res) * (2.0f - res);
        }

        // New soft-shadow technique:
        // Reference: https://iquilezles.org/articles/rmshadows/
        // New Version: https://www.shadertoy.com/view/tscSRS
        private float2 SoftShadow2(float3 point, float3 dir, float start, float end)
        {
            float depth = start, dist;
            float shadow = 1f;
            float closestObj = -1;
            for (int i = 0; i < worldData[0].maxShadowRaySteps; ++i)
            {
                float2 sdf = WorldSDF(point + depth * dir, true);
                dist = sdf.X;
                closestObj = sdf.Y;
                if (depth > end || shadow < -1.0)
                    break;

                shadow = Hlsl.Min(shadow, 40f * dist / depth);
                depth += Hlsl.Clamp(dist, 0.005f, 10f);
            }

            shadow = Hlsl.Max(shadow, -1f);
            return new float2(Hlsl.SmoothStep(-1f, 0f, shadow), closestObj);
        }

        private float2 SoftShadowCambridge(float3 lightPos, float3 hitPoint, float renderDepth)
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
        }

        // PBR functions: https://chat.deepseek.com/share/bbtq3pqgcx353c6yqw
        // Fresnel reflectance (Schlick's approximation)
        private float3 FresnelSchlickRGB(float cosTheta, float3 f0)
        {
            return f0 + (new float3(1f, 1f, 1f) - f0) * Hlsl.Pow(1.0f - cosTheta, 5.0f);
        }

        // Get material F0 for Fresnel calculations
        private float3 GetMaterialF0(int objIndex)
        {
            float metallic = sdfPrimitives[objIndex].metallic;
            float specular = sdfPrimitives[objIndex].specular;
            float3 albedo = sdfPrimitives[objIndex].color.XYZ;

            float dielectricF0 = 0.04f * specular;
            return Hlsl.Lerp(new float3(dielectricF0, dielectricF0, dielectricF0), albedo, metallic); 
        }

        // Reference: https://chat.deepseek.com/share/qk6oisykt5bop6h9hn
        // GGX/Towbridge-Reitz normal distribution function (D term)
        private float GGX_Distribution(float NdotH, float roughness)
        {
            float a2 = roughness * roughness;
            a2 = a2 * a2; // roughness^4 for perceptually linear roughness
            float denom = NdotH * NdotH * (a2 - 1.0f) + 1.0f;
            return a2 / (3.14159265f * denom * denom);
        }

        // Schlick-GGX geometry function (G term) with Smith's method
        private float GGX_Geometry(float NdotV, float roughness)
        {
            float r = roughness + 1.0f;
            float k = r * r / 8.0f; // Direct lighting
            float denom = NdotV * (1.0f - k) + k;
            return NdotV / denom;
        }

        // Cook-Torrance specular BRDF
        private float3 CookTorranceSpecular(float3 normal, float3 viewDir, float3 lightDir, float3 halfwayDir, float roughness, float3 fresnelRGB)
        {
            float NdotV = Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f);
            float NdotL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
            float NdotH = Hlsl.Max(Hlsl.Dot(normal, halfwayDir), 0f);
            //float VdotH = Hlsl.Max(Hlsl.Dot(viewDir, halfwayDir), 0f);

            // Early exit if surface is backfacing
            if (NdotL <= 0.0f || NdotV <= 0.0f)
                return float3.Zero;

            float D = GGX_Distribution(NdotH, roughness); // Distribution
            float G = GGX_Geometry(NdotV, roughness) * GGX_Geometry(NdotL, roughness); // Geometry

            // Cook-Torrance specular BRDF
            return D * G * fresnelRGB / Hlsl.Max(4.0f * NdotV * NdotL, EPSILON);
        }

        /*// Adapted from: https://github.com/pboechat/cook_torrance/blob/master/application/shaders/cook_torrance_colored.fs.glsl
        private float3 CookTorrance(float3 materialDiffuseColor,
            float3 materialSpecularColor,
            float3 normal,
            float3 lightDir,
            float3 viewDir,
            float3 lightColor,
            float roughness,
            float f0)
        {
            float NdotL = Hlsl.Max(0, Hlsl.Dot(normal, lightDir));
            float Rs = 0.0f;
            if (NdotL > 0)
            {
                float3 H = Hlsl.Normalize(lightDir + viewDir);
                float NdotH = Hlsl.Max(0, Hlsl.Dot(normal, H));
                float NdotV = Hlsl.Max(0, Hlsl.Dot(normal, viewDir));
                float VdotH = Hlsl.Max(0, Hlsl.Dot(lightDir, H));

                // Fresnel reflectance
                float F = Hlsl.Pow(1.0f - VdotH, 5.0f);
                F *= 1.0f - f0;
                F += f0;

                // Microfacet distribution by Beckmann
                float m_squared = roughness * roughness;
                float r1 = 1.0f / (4.0f * m_squared * Hlsl.Pow(NdotH, 4.0f));
                float r2 = (NdotH * NdotH - 1.0f) / (m_squared * NdotH * NdotH);
                float D = r1 * Hlsl.Exp(r2);

                // Geometric shadowing
                float two_NdotH = 2.0f * NdotH;
                float g1 = two_NdotH * NdotV / VdotH;
                float g2 = two_NdotH * NdotL / VdotH;
                float G = Hlsl.Min(1.0f, Hlsl.Min(g1, g2));

                Rs = (F * D * G) / (3.1415926f * NdotL * NdotV);
            }
            return materialDiffuseColor * lightColor * NdotL + lightColor * materialSpecularColor * Rs;
        }*/

        private float3 DebugBRDF(float3 N, float3 V, float3 L, float3 H, float roughness, float3 F0)
        {
            float NdotV = Hlsl.Max(Hlsl.Dot(N, V), 0.0f);
            float NdotL = Hlsl.Max(Hlsl.Dot(N, L), 0.0f);
            float NdotH = Hlsl.Max(Hlsl.Dot(N, H), 0.0f);

            float D = GGX_Distribution(NdotH, roughness);
            float G = GGX_Geometry(NdotV, roughness) * GGX_Geometry(NdotL, roughness);
            float3 F = FresnelSchlickRGB(Hlsl.Max(Hlsl.Dot(V, H), 0.0f), F0);

            // Return RGB with: R = D, G = G, B = average(F)
            return new float3(D, G, (F.X + F.Y + F.Z) / 3.0f);
        }

        // Depth of Field section:
        private float2 RandomPointOnDisk(uint seed, float2 pixel)
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
        }

        /// <summary>
        /// Actually performs the main raymarching calculations.
        /// </summary>
        /// <param name="rayOrigin">Ray origin to start at</param>
        /// <param name="rayDir">Ray direction to travel</param>
        /// <returns>Output raymarch color, with effects</returns>
        private float3 TraceRay(int2 pixel, float3 rayOrigin, float3 rayDir, out float3 outputNormal, out float totalDist)
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
                if (totalDist > farClipPlane)
                    break;
            }
            float stepCost = step / (float)maxSteps;

            if (closestObjIndex > -1)
            {
                // Calculate objectColor, lighting, normals, etc. eventually
                float3 normal = FastNormal(hitPoint);
                float3 viewDir = -rayDir;

                // Update data buffers
                outputNormal = normal;
                objectIdBuffer[pixel.X + pixel.Y * (int)width] = closestObjIndex;

                // Get material
                float3 albedoColor = sdfPrimitives[closestObjIndex].color.RGB;
                float metallic = sdfPrimitives[closestObjIndex].metallic;
                float roughness = sdfPrimitives[closestObjIndex].roughness;
                float ior = sdfPrimitives[closestObjIndex].ior;
                float ao = sdfPrimitives[closestObjIndex].ao;
                float3 F0 = GetMaterialF0(closestObjIndex);

                // Default light values
                float ambientLightAmt = 0.15f;

                // Light vectors
                float3 lightDir = Hlsl.Normalize(sunDir);
                float3 halfVec = Hlsl.Normalize(viewDir + lightDir);

                // Fresnel
                float VdotH = Hlsl.Max(Hlsl.Dot(viewDir, halfVec), 0f);
                float3 fresnel = FresnelSchlickRGB(VdotH, F0);

                // Specular term
                float3 specular = CookTorranceSpecular(normal, viewDir, lightDir, halfVec, roughness, F0);

                // Shadows
                float2 shadowValues = new float2(1f, 0f);
                float3 shadowOrigin = hitPoint + normal * EPSILON;
                float2 shadowDistances = sdfPrimitives[closestObjIndex].shadowDistances;
                if (sdfPrimitives[closestObjIndex].shadowEffects.Y)
                    shadowValues = SoftShadow2(shadowOrigin, lightDir, shadowDistances.X, shadowDistances.Y);
                    //shadowValues = SoftShadowCambridge(shadowOrigin, lightDir * 100000f, shadowDistances.Y);

                // Dot products
                //float NdotV = Hlsl.Max(Hlsl.Dot(normal, viewDir), 0.0f);
                float NdotL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);

                // Multiple scattering compensation (for rough surfaces)
                float3 energyCompensation = 1f + roughness * (1f - metallic);
                specular *= energyCompensation;

                // Diffuse term
                float3 diffuse = float3.Zero;
                if (metallic < 0.99f) // Energy conservation
                {
                    float3 kD = (new float3(1f, 1f, 1f) - fresnel) * (1f - metallic);
                    diffuse = kD * albedoColor / 3.14159265f;
                }

                // Ambient occlusion
                float aoAmt = 1f;
                /*if (ao > 0.001f)
                {
                    float3 aoPoint = hitPoint + normal * EPSILON;
                    float stepDist = 0.05f;

                    aoAmt = CalculatePhysicallyBasedAO(pixel, aoPoint, normal);

                    // Blend with material's AO strength
                    aoAmt = Hlsl.Lerp(1f, aoAmt, ao);
                    //aoAmt = Hlsl.Lerp(0f, aoAmt, 1f - Hlsl.Clamp(shadowValues.X, 0, 1));
                }*/

                // Lighting
                float3 directLighting = (diffuse + specular) * NdotL * shadowValues.X;
                float3 ambient = albedoColor * ambientLightAmt * aoAmt * (1f - metallic);

                // Final color (NO extra kD multiplication!)
                outputColor = ambient + directLighting;

                // Debug shadows
                if (outputMode == 2) outputColor = new float3(shadowValues.X, shadowValues.Y / sdfPrimitives.Length, 0f);
                else if (outputMode == 3) outputColor = DebugBRDF(normal, viewDir, lightDir, halfVec, roughness, F0);
            }

            if (outputMode == 1) outputColor = new float3(stepCost, stepCost, stepCost); // Debug ray steps
            return outputColor;
        }

        /// <summary>
        /// Executes the raymarching sequence.
        /// </summary>
        public void Execute()
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
                uint seed = (uint)(pixelCoord.X * 1973 + pixelCoord.Y * 9277 + sample * 26699 + (uint)frame);

                // Get original ray direction
                float3 rayDir = GetCameraRayDir(uv);

                // Apply Depth of Field if enabled
                if (apertureSize > 0.001f)
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
                }

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
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
