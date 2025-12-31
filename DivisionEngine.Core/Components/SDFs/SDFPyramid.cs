namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a pyramid SDF.
    /// </summary>
    public class SDFPyramid : IComponent
    {
        /// <summary>
        /// Pyramid with height of 2.0.
        /// </summary>
        public SDFPyramid()
        {
            height = 2f;
        }

        public float height;
    }
}
