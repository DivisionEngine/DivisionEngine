using ComputeSharp;
using DivisionEngine.Rendering;
using Silk.NET.Input;

#pragma warning disable CA1416 // Validate platform compatibility
namespace DivisionEngine
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFShader(
        ReadWriteTexture2D<float4> texture,
        float width,
        float height,
        ReadOnlyBuffer<SDFWorldDTO> worldData,
        ReadOnlyBuffer<SDFPrimitiveObjectDTO> sdfPrimitives) : IComputeShader
    {

        const float MAX_RAYMARCH_DISTANCE = 10000.0f;
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
            objPos *= scale;
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
                else // Default sphere SDF
                    dist = SphereSDF(transformedPt, sdfPrimitives[i].parameters.X);

                if (dist < minDist)
                {
                    closest = i;
                    minDist = dist;
                }
            }

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

        // Calculates the lighting amount based on a normal vector
        private float NormalLighting(float3 normal)
        {
            return Hlsl.Clamp(Hlsl.Dot(Hlsl.Normalize(sunDir), normal), 0, 1);
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

        // New soft-shadow technique:
        // Reference: https://iquilezles.org/articles/rmshadows/
        private float SoftShadow2(float3 rayOrigin, float3 rayDir, float mint, float maxt, float lightAngle)
        {
            float res = 1.0f;
            float t = mint;
            for (int i = 0; i < worldData[0].maxShadowRaySteps && t < maxt; i++)
            {
                float h = WorldSDF(rayOrigin + t * rayDir, true).X;
                res = Hlsl.Min(res, h / (lightAngle * t));
                t += Hlsl.Clamp(h, 0.005f, 0.50f);
                if (res < -1.0f || t > maxt) break;
            }
            res = Hlsl.Max(res, -1.0f);
            return 0.25f * (1.0f + res) * (1.0f + res) * (2.0f - res);
        }

        private float3 GetMaterialColor(int objIndex)
        {
            if (objIndex < 0 || objIndex > sdfPrimitives.Length)
                return new float3(0.0f, 0.0f, 0.0f);

            return sdfPrimitives[objIndex].color.XYZ;
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY; // Get pixel position
            texture[pixel] = new float4(0, 0, 0, 0); // Clear render texture

            // Get uv coord
            float2 uv = (float2)pixel / new float2(width, height) * 2.0f - 1.0f;
            uv.X *= width / height;
            float3 rayDir = GetCameraRayDir(uv);
            float3 rayOrigin = worldData[0].cameraOrigin;

            // SDF Raymarch Pass
            float totalDist = 0.0f;
            int closestObjIndex = -1;
            float3 outputColor = new float3(0f, 0f, 0f);
            float3 hitPoint = rayOrigin;

            int maxSteps = worldData[0].maxRaySteps;
            for (int step = 0; step < maxSteps; step++)
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
                if (totalDist > MAX_RAYMARCH_DISTANCE)
                    break;
            }

            if (closestObjIndex > -1)
            {
                // Calculate objectColor, lighting, normals, etc. eventually
                float3 normal = FastNormal(hitPoint);

                float ambientLightAmt = 0.05f;
                float diffuseLightAmt = NormalLighting(normal);
                float shadowAmt = 1.0f;

                float3 shadowOrigin = hitPoint + normal * EPSILON;

                float2 shadowDistances = sdfPrimitives[closestObjIndex].shadowDistances;
                if (sdfPrimitives[closestObjIndex].shadowEffects.Y)
                    shadowAmt = SoftShadow2(shadowOrigin, Hlsl.Normalize(sunDir), shadowDistances.X, shadowDistances.Y, 0.25f);

                float3 materialColor = GetMaterialColor(closestObjIndex);

                outputColor = materialColor * (ambientLightAmt + diffuseLightAmt * shadowAmt);
            }

            texture[pixel] = new float4(outputColor, 1.0f);
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
