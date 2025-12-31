namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a SDF sphere.
    /// </summary>
    public class SDFSphere : IComponent
    {
        /// <summary>
        /// Sphere with a radius of 1.0.
        /// </summary>
        public SDFSphere()
        {
            radius = 1f;
        }

        public float radius;
    }
}
