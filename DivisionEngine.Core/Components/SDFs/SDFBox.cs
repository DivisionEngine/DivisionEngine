using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a box SDF.
    /// </summary>
    public class SDFBox : IComponent
    {
        /// <summary>
        /// White box with size of 1.0.
        /// </summary>
        public SDFBox()
        {
            color = ColorPalette.White;
            size = new float3(1f, 1f, 1f);
        }

        [Color(false)] public float4 color;
        public float3 size;
    }
}
