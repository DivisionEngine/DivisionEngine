namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a pyramid SDF.
    /// </summary>
    public class SDFPyramid : IComponent
    {
        /// <summary>
        /// White pyramid with height of 1.0.
        /// </summary>
        public SDFPyramid()
        {
            color = new float4(1f, 1f, 1f, 1f);
            height = 1f;
        }

        public float4 color;
        public float height;
    }
}
