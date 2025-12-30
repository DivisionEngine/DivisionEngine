using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a capsule SDF.
    /// </summary>
    public class SDFCapsule : IComponent
    {
        /// <summary>
        /// White capsule with radius of 1.0 and height of 3.0.
        /// </summary>
        public SDFCapsule()
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
