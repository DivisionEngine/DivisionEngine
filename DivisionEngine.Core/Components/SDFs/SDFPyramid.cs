using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a pyramid SDF.
    /// </summary>
    public class SDFPyramid : IComponent
    {
        /// <summary>
        /// White pyramid with height of 2.0.
        /// </summary>
        public SDFPyramid()
        {
            color = ColorPalette.White;
            height = 2f;
        }

        [Color(false)] public float4 color;
        public float height;
    }
}
