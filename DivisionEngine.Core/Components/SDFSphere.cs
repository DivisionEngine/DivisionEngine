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
        public static SDFSphere Default => new SDFSphere
        {
            color = float4.One,
            radius = 1f
        };

        public float4 color;
        public float radius;
    }
}
