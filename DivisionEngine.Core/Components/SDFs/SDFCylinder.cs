using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a cylinder SDF.
    /// </summary>
    public class SDFCylinder : IComponent
    {
        /// <summary>
        /// White cylinder with radius 1.0 and height 3.0.
        /// </summary>
        public SDFCylinder()
        {
            color = ColorPalette.White;
            height = 3f;
            radius = 1f;
        }

        [Color(false)] public float4 color;
        public float height;
        public float radius;
    }
}
