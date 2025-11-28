namespace DivisionEngine.Components.SDFs
{
    public class SDFRoundedBox : IComponent
    {
        public SDFRoundedBox()
        {
            color = new float4(1f, 1f, 1f, 1f);
            size = new float3(1f, 1f, 1f);
            bevel = 0.05f;
        }

        public float4 color;
        public float3 size;
        public float bevel;
    }
}
