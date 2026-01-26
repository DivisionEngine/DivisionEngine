namespace DivisionEngine.Components.SDFs.Primitives
{
    /// <summary>
    /// Represents a cone SDF.
    /// </summary>
    public class SDFCone : IComponent
    {
        /// <summary>
        /// Cone with angles of 0.6 and 0.4 and height of 3.0.
        /// </summary>
        public SDFCone()
        {
            cone = new float2(0.6f, 0.4f);
            height = 3f;
        }

        public float2 cone;
        public float height;

        public IComponent Clone() => new SDFCone
        {
            cone = cone,
            height = height,
        };
    }
}
