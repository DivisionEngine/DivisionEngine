namespace DivisionEngine.Components.SDFs.Primitives
{
    /// <summary>
    /// Represents a cylinder SDF.
    /// </summary>
    public class SDFCylinder : IComponent
    {
        /// <summary>
        /// Cylinder with radius 1.0 and height 3.0.
        /// </summary>
        public SDFCylinder()
        {
            height = 3f;
            radius = 1f;
        }

        public float height;
        public float radius;

        public IComponent Clone() => new SDFCylinder
        {
            height = height,
            radius = radius,
        };
    }
}
