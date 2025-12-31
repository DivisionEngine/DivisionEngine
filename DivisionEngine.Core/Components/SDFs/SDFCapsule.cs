namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a capsule SDF.
    /// </summary>
    public class SDFCapsule : IComponent
    {
        /// <summary>
        /// Capsule with radius of 1.0 and height of 3.0.
        /// </summary>
        public SDFCapsule()
        {
            height = 3f;
            radius = 1f;
        }

        public float height;
        public float radius;
    }
}
