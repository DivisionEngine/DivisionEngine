using ComputeSharp;

#pragma warning disable CA1416 // Validate platform compatibility
namespace DivisionEngine
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFShader(
        ReadWriteTexture2D<float4> texture,
        float time,
        float width,
        float height,
        float4x4 camToWorld,
        float4x4 camInverseProj) : IComputeShader
    {
        //float rectangle(float2 samplePosition, float2 halfSize)
        //{
        //    float2 componentWiseEdgeDistance = Hlsl.Abs(samplePosition) - halfSize;
        //    float outsideDistance = Hlsl.Length(Hlsl.Max(componentWiseEdgeDistance, 0));
        //    float insideDistance = Hlsl.Min(Hlsl.Max(componentWiseEdgeDistance.X, componentWiseEdgeDistance.Y), 0);
        //    return outsideDistance + insideDistance;
        //}

        private float SphereSDF(float3 pt, float3 center, float rad)
        {
            return Hlsl.Length(pt - center) - rad;
        }

        private float3 GetCamRayDir(float2 coord)
        {
            return Hlsl.Normalize(Hlsl.Mul(camToWorld, new float4(Hlsl.Mul(camInverseProj, new float4(coord, 0.0f, 1.0f)).XYZ, 0.0f)).XYZ);
        }

        public void Execute()
        {
            int2 pos = ThreadIds.XY;

            texture[pos] = new float4(0, 0, 0, 0);

            float2 percentOnTex = (float2)pos / new float2(width, height) * 2.0f - 1.0f;
            float3 rayDir = GetCamRayDir(percentOnTex * 2 - 1);

            texture[pos] = new float4(1, 0.5f + time, time, 1);
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
