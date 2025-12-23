using DivisionEngine.Components.FieldAttributes;

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
            color = new float4(1f, 1f, 1f, 1f);
            size = new float3(1f, 1f, 1f);
        }

        [Color] public float4 color;
        public float3 size;
    }
}
