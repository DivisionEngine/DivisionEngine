namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a plane SDF.
    /// </summary>
    public class SDFPlane : IComponent
    {
        /// <summary>
        /// White plane with a normal vector directly up and a height of 1.0.
        /// </summary>
        public SDFPlane()
        {
            color = new float4(1f, 1f, 1f, 1f);
            normal = new float3(0f, 1f, 0f);
            h = 1f;
        }

        public float4 color;
        public float3 normal;
        public float h;
    }
}
