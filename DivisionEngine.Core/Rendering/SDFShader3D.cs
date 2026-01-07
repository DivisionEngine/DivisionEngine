using ComputeSharp;
using DivisionEngine.Rendering;
using Silk.NET.OpenGL;

#pragma warning disable CA1416 // Validate platform compatibility
namespace DivisionEngine
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFShader3D(
        float width,
        float height,
        int outputMode,
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
        private float3 ApplyTransforms(float3 pt, float3 position, float4 rotation, float3 scale)
        {
            float3 objPos = pt - position;
            objPos = RotateVector(objPos, rotation);
            objPos /= Hlsl.Max(scale, new float3(EPSILON, EPSILON, EPSILON)); // Make sure not dividing by 0.
            return objPos;
        }

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

        private float2 WorldSDF(float3 point, bool shadowCastCheck)
        {
            float minDist = MIN_TRAVERSE_DIST;

            int closest = -1;
            float3 transformedPt;
            for (int i = 0; i < sdfPrimitives.Length; i++)
            {
                if (shadowCastCheck && !sdfPrimitives[i].shadowEffects.X) continue;
                transformedPt = ApplyTransforms(point, sdfPrimitives[i].position, sdfPrimitives[i].rotation, sdfPrimitives[i].scaling);

                float dist;
                if (sdfPrimitives[i].type == 0) // Adds sphere SDFs
                    dist = SphereSDF(transformedPt, sdfPrimitives[i].parameters.X);
                else if (sdfPrimitives[i].type == 1) // Adds box SDFs
                    dist = BoxSDF(transformedPt, sdfPrimitives[i].parameters.XYZ);
                else if (sdfPrimitives[i].type == 2) // Adds rounded box SDFs
                    dist = RoundedBoxSDF(transformedPt, sdfPrimitives[i].parameters.XYZ, sdfPrimitives[i].parameters.W);
                else if (sdfPrimitives[i].type == 3) // Adds torus SDFs
                    dist = TorusSDF(transformedPt, sdfPrimitives[i].parameters.XY);
                else if (sdfPrimitives[i].type == 4) // Adds pyramid SDFs
                    dist = PyramidSDF(transformedPt, sdfPrimitives[i].parameters.X);
                else if (sdfPrimitives[i].type == 5) // Adds plane SDFs
                    dist = PlaneSDF(transformedPt, sdfPrimitives[i].parameters.XYZ, sdfPrimitives[i].parameters.W);
                else if (sdfPrimitives[i].type == 6) // Adds cylinder SDFs
                    dist = CylinderSDF(transformedPt, sdfPrimitives[i].parameters.X, sdfPrimitives[i].parameters.Y);
                else if (sdfPrimitives[i].type == 7) // Adds capsule SDFs
                    dist = CapsuleSDF(transformedPt, sdfPrimitives[i].parameters.X, sdfPrimitives[i].parameters.Y);
                else if (sdfPrimitives[i].type == 8) // Adds cone SDFs
                    dist = ConeSDF(transformedPt, sdfPrimitives[i].parameters.XY, sdfPrimitives[i].parameters.Z);
                else // Default to sphere SDF
                    dist = SphereSDF(transformedPt, sdfPrimitives[i].parameters.X);

                if (dist < minDist)
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
        private float2 SoftShadow2(float3 rayOrigin, float3 rayDir, float mint, float maxt, float lightAngle)
        {
            float res = 1.0f;
            float t = mint;
            float2 h = new float2(0, 0);
            for (int i = 0; i < worldData[0].maxShadowRaySteps && t < maxt; i++)
            {
                h = WorldSDF(rayOrigin + t * rayDir, true);
                res = Hlsl.Min(res, h.X / (lightAngle * t));
                t += Hlsl.Clamp(h.X, 0.005f, 0.50f);
                if (res < -1.0f || t > maxt) break;
            }
            res = Hlsl.Max(res, -1.0f);
            return new float2(0.25f * (1.0f + res) * (1.0f + res) * (2.0f - res), h.Y);
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

        public void Execute()
        {
            int2 pixel = ThreadIds.XY; // Get pixel position
            texture[pixel] = new float4(0, 0, 0, 0); // Clear render texture
            depthNormals[pixel] = new float4(0, 0, 0, 0); // Clear depth and normal texture
            objectIdBuffer[pixel.X + pixel.Y * (int)width] = -1; // Clear object ID buffer

            // Get uv coord
            float2 uv = (float2)pixel / new float2(width, height) * 2.0f - 1.0f;
            uv.X *= width / height;
            float3 rayDir = GetCameraRayDir(uv);
            float3 rayOrigin = worldData[0].cameraOrigin;

            // SDF raymarch variables
            float totalDist = worldData[0].nearPlane; // Start at near clip plane
            float farClipPlane = worldData[0].farPlane;
            int closestObjIndex = -1; // Clear initial object index
            float3 outputColor = worldData[0].backgroundColor.XYZ; // Set output skybox color
            float3 hitPoint = rayOrigin;

            // SDF depth and normal variables
            float3 outputNormal = new float3(0, 0, 0);

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
                float lightAngle = 0.25f;

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
                    shadowValues = SoftShadow2(shadowOrigin, lightDir, shadowDistances.X, shadowDistances.Y, lightAngle);

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

                // Lighting
                float3 directLighting = (diffuse + specular) * NdotL * shadowValues.X;
                float aoAmt = Hlsl.Lerp(1f, stepCost, ao); // fix this in the future
                float3 ambient = albedoColor * ambientLightAmt * (1f - metallic) * aoAmt;

                // Final color (NO extra kD multiplication!)
                outputColor = ambient + directLighting;
                
                // Debug shadows
                if (outputMode == 2) outputColor = new float3(shadowValues.X, shadowValues.Y / sdfPrimitives.Length, 0f);
                else if (outputMode == 3) outputColor = DebugBRDF(normal, viewDir, lightDir, halfVec, roughness, F0);
            }

            if (outputMode == 1) outputColor = new float3(stepCost, stepCost, stepCost); // Debug ray steps
            texture[pixel] = new float4(outputColor, 1f);
            depthNormals[pixel] = new float4(totalDist / (farClipPlane - worldData[0].nearPlane), outputNormal);
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
