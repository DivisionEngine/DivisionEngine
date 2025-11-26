using DivisionEngine.MathLib;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Component for translation, rotation, and scaling.
    /// </summary>
    public class Transform : IComponent
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

        public float3 Forward => new float3(0, 0, -1).Transform(rotation).Normalize();
        public float3 Back => new float3(0, 0, 1).Transform(rotation).Normalize();
        public float3 Up => new float3(0, 1, 0).Transform(rotation).Normalize();
        public float3 Down => rotation.RotateVector(new float3(0, -1, 0)).Normalize();
        public float3 Left => rotation.RotateVector(new float3(-1, 0, 0)).Normalize();
        public float3 Right => new float3(1, 0, 0).Transform(rotation).Normalize();
    }
}
