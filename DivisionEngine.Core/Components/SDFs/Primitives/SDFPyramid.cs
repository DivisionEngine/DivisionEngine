namespace DivisionEngine.Components.SDFs.Primitives
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

        public IComponent Clone() => new SDFPyramid
        {
            height = height,
        };
    }
}
