namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a SDF donut shape.
    /// </summary>
    public class SDFTorus : IComponent
    {
        /// <summary>
        /// Creates a new default torus.
        /// </summary>
        public SDFTorus()
        {
            color = new float4(1f, 1f, 1f, 1f);
            wholeRadius = 2f;
            ringRadius = 1f;
        }

        public float4 color;
        public float wholeRadius;
        public float ringRadius;
    }
}
