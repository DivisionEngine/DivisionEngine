namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a plane SDF.
    /// </summary>
    public class SDFPlane : IComponent
    {
        /// <summary>
        /// Plane with a normal vector directly up and a height of 1.0.
        /// </summary>
        public SDFPlane()
        {
            normal = new float3(0f, 1f, 0f);
            h = 1f;
        }

        public float3 normal;
        public float h;
    }
}
