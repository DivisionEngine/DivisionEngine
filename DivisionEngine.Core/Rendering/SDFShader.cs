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

        const int MAX_RAYMARCH_STEPS = 128;
        const float MAX_RAYMARCH_DISTANCE = 1000.0f;
        const float EPSILON = 0.0001f;
        const float MIN_TRAVERSE_DIST = 100000000.0f;

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

        private float SphereSDF(float3 pt, float3 center, float rad)
        {
            return Hlsl.Length(pt - center) - rad;
        }

        private float WorldSDF(float3 point)
        {
            float minDist = MIN_TRAVERSE_DIST;

            for (int i = 0; i < sdfPrimitives.Length; i++)
            {
                // Replace with primitive evaluation function in future
                float dist = SphereSDF(point, sdfPrimitives[i].position, sdfPrimitives[i].parameters.X);
                minDist = Hlsl.Min(minDist, dist);
            }

            return minDist;
        }

        private int FindClosestObj(float3 hitPoint)
        {
            int closest = -1;
            float minDist = EPSILON;

            for (int i = 0; i < sdfPrimitives.Length; i++)
            {
                // Replace with primitive evaluation function in future
                float dist = SphereSDF(hitPoint, sdfPrimitives[i].position, sdfPrimitives[i].parameters.X);
                if (dist < minDist)
                {
                    closest = i;
                    minDist = dist;
                }
            }

            return closest;
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
                float worldDist = WorldSDF(hitPoint);
                if (worldDist < EPSILON)
                {
                    closestObjIndex = FindClosestObj(hitPoint);
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

                outputColor = new float3(1f, 1f, 1f);
            }

            texture[pixel] = new float4(outputColor, 1.0f);
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
