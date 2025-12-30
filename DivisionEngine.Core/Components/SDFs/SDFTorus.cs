using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

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
            color = ColorPalette.White;
            wholeRadius = 2f;
            ringRadius = 1f;
        }

        [Color(false)] public float4 color;
        public float wholeRadius;
        public float ringRadius;
    }
}
