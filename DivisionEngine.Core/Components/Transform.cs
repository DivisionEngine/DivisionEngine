using DivisionEngine.Math;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Component for translation, rotation, and scaling.
    /// </summary>
    public struct Transform : IComponent
    {
        /// <summary>
        /// Sets the position to (0, 0, 0), rotation to identity quaternion, and scaling to (1, 1, 1).
        /// </summary>
        public static Transform Default => new Transform
        {
            position = float3.Zero,
            rotation = Quaternion.Identity,
            scaling = float3.One
        };

        public float3 position;
        public float4 rotation;
        public float3 scaling;
    }
}
