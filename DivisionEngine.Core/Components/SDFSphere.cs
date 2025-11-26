namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents a SDF sphere.
    /// </summary>
    public class SDFSphere : IComponent
    {
        /// <summary>
        /// White sphere with a radius of 1.0.
        /// </summary>
        public SDFSphere()
        {
            color = new float4(1f, 1f, 1f, 1f);
            radius = 1f;
        }

        public float4 color;
        public float radius;
    }
}
