namespace DivisionEngine.Components
{
    public struct DirectionalLight : IComponent
    {
        public float4 color;
        public float3 direction;
        public float intensity;
    }
}
