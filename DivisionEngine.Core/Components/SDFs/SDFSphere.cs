using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents a SDF sphere.
    /// </summary>
    public class SDFSphere : IComponent
    {
        /// <summary>
        /// White sphere with a radius of 1.0.
        /// </summary>
        public SDFSphere()
        {
            color = ColorPalette.White;
            radius = 1f;
        }

        [Color(false)] public float4 color;
        public float radius;
    }
}
