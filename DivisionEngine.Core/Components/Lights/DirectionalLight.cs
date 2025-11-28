namespace DivisionEngine.Components.Lights
{
    public class DirectionalLight : IComponent
    {
        public float4 color;
        public float3 direction;
        public float intensity;
    }
}
