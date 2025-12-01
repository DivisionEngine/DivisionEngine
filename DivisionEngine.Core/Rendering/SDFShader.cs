using ComputeSharp;
using DivisionEngine.Rendering;

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

        const int MAX_RAYMARCH_STEPS = 200;
        const float MAX_RAYMARCH_DISTANCE = 10000.0f;
        const float EPSILON = 0.0001f;
        const float MIN_TRAVERSE_DIST = 100000000.0f;
        const float resolution = 0.001f;
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

        /*private float3 ApplyTransformations(float3 pt, float3 translation, float4 rotation)
        {
            
        }*/

        private float SphereSDF(float3 pt, float3 center, float r)
        {
            return Hlsl.Length(pt - center) - r;
        }

        private float BoxSDF(float3 pt, float3 center, float3 size)
        {
            float3 q = Hlsl.Abs(pt - center) - size;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f);
        }

        private float RoundedBoxSDF(float3 pt, float3 center, float3 size, float r)
        {
            float3 q = Hlsl.Abs(pt - center) - size + r;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f) - r;
        }

        private float TorusSDF(float3 pt, float3 center, float2 tr)
        {
            float3 p = pt - center;
            float2 q = new float2(Hlsl.Length(p.XZ) - tr.X, p.Y);
            return Hlsl.Length(q) - tr.Y;
        }

        private float2 WorldSDF(float3 point, bool shadowCastCheck)
        {
            float minDist = MIN_TRAVERSE_DIST;

            int closest = -1;
            for (int i = 0; i < sdfPrimitives.Length; i++)
            {
                if (shadowCastCheck && !sdfPrimitives[i].shadowEffects.X) continue;

                float dist;
                if (sdfPrimitives[i].type == 0)
                    dist = SphereSDF(point, sdfPrimitives[i].position, sdfPrimitives[i].parameters.X);
                else if (sdfPrimitives[i].type == 1)
                    dist = BoxSDF(point, sdfPrimitives[i].position, sdfPrimitives[i].parameters.XYZ);
                else if (sdfPrimitives[i].type == 2)
                    dist = RoundedBoxSDF(point, sdfPrimitives[i].position,
                        sdfPrimitives[i].parameters.XYZ, sdfPrimitives[i].parameters.W);
                else if (sdfPrimitives[i].type == 3)
                    dist = TorusSDF(point, sdfPrimitives[i].position, sdfPrimitives[i].parameters.XY);
                else
                    dist = SphereSDF(point, sdfPrimitives[i].position, sdfPrimitives[i].parameters.X);

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
                n += e * WorldSDF(pos + resolution * 50 * e, false).X;
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

            for (int step = 0; step < MAX_RAYMARCH_STEPS; step++)
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

                if (sdfPrimitives[closestObjIndex].shadowEffects.Y)
                    shadowAmt = SoftShadow(shadowOrigin, Hlsl.Normalize(sunDir), 0.001f, 10f);

                float3 materialColor = GetMaterialColor(closestObjIndex);

                outputColor = materialColor * (ambientLightAmt + diffuseLightAmt * shadowAmt);
            }

            texture[pixel] = new float4(outputColor, 1.0f);
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
