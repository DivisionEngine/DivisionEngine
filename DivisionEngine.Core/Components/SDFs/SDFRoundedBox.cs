namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a rounded box SDF.
    /// </summary>
    public class SDFRoundedBox : IComponent
    {
        /// <summary>
        /// Rounded box with a size of 1.0 and a bevel of 0.05.
        /// </summary>
        public SDFRoundedBox()
        {
            size = new float3(1f, 1f, 1f);
            bevel = 0.05f;
        }

        public float3 size;
        public float bevel;
    }
}
