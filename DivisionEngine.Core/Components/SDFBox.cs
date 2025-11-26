namespace DivisionEngine.Components
{
    public class SDFBox : IComponent
    {
        public SDFBox()
        {
            color = new float4(1f, 1f, 1f, 1f);
            size = new float3(1f, 1f, 1f);
        }

        public float4 color;
        public float3 size;
    }
}
