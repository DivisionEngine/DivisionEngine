using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a plane SDF.
    /// </summary>
    public class SDFPlane : IComponent
    {
        /// <summary>
        /// White plane with a normal vector directly up and a height of 1.0.
        /// </summary>
        public SDFPlane()
        {
            color = ColorPalette.White;
            normal = new float3(0f, 1f, 0f);
            h = 1f;
        }

        [Color(false)] public float4 color;
        public float3 normal;
        public float h;
    }
}
