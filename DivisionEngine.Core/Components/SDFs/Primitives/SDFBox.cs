namespace DivisionEngine.Components.SDFs.Primitives
{
    /// <summary>
    /// Represents a box SDF.
    /// </summary>
    public class SDFBox : IComponent
    {
        /// <summary>
        /// Box with size of 1.0.
        /// </summary>
        public SDFBox()
        {
            size = new float3(1f, 1f, 1f);
        }

        public float3 size;

        public IComponent Clone() => new SDFBox
        {
            size = size,
        };
    }
}
