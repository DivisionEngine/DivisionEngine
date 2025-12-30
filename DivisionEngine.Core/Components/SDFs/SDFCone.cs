using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a cone SDF.
    /// </summary>
    public class SDFCone : IComponent
    {
        /// <summary>
        /// White cone with angles of 0.6 and 0.4 and height of 3.0.
        /// </summary>
        public SDFCone()
        {
            color = ColorPalette.White;
            cone = new float2(0.6f, 0.4f);
            height = 3f;
        }

        [Color(false)] public float4 color;
        public float2 cone;
        public float height;
    }
}
