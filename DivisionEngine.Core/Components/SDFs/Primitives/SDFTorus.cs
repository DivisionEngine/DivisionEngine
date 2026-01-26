namespace DivisionEngine.Components.SDFs.Primitives
{
    /// <summary>
    /// Represents a SDF donut shape.
    /// </summary>
    public class SDFTorus : IComponent
    {
        /// <summary>
        /// Torus with whole radius of 2.0 and ring radius of 1.0.
        /// </summary>
        public SDFTorus()
        {
            wholeRadius = 2f;
            ringRadius = 1f;
        }

        public float wholeRadius;
        public float ringRadius;

        public IComponent Clone() => new SDFTorus
        {
            wholeRadius = wholeRadius,
            ringRadius = ringRadius,
        };
    }
}
