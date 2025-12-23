using DivisionEngine.Components.FieldAttributes;

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
            color = new float4(1f, 1f, 1f, 1f);
            height = 3f;
            radius = 1f;
        }

        [Color] public float4 color;
        public float height;
        public float radius;
    }
}
