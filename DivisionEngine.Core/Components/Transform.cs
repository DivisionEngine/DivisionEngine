using DivisionEngine.Components.FieldAttributes;
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
        public Transform()
        {
            position = new float3(0f, 0f, 0f);
            rotation = Quaternion.Identity;
            scaling = new float3(1f, 1f, 1f);
        }

        public float3 position;
        [Rotation(true)] public float4 rotation;
        public float3 scaling;

        public float3 Forward => rotation.RotateVector(new float3(0, 0, -1)).Normalize();
        public float3 Back => rotation.RotateVector(new float3(0, 0, 1)).Normalize();
        public float3 Up => rotation.RotateVector(new float3(0, 1, 0)).Normalize();
        public float3 Down => rotation.RotateVector(new float3(0, -1, 0)).Normalize();
        public float3 Left => rotation.RotateVector(new float3(-1, 0, 0)).Normalize();
        public float3 Right => rotation.RotateVector(new float3(1, 0, 0)).Normalize();

        /*private static float3x3 RotAxis(float3 axis, float angle)
        {
            float s = Math.Sin(angle);
            float c = Math.Cos(angle);
            float oc = 1f - c;
            return new float3x3(
                oc * axis.X * axis.X + c, oc * axis.X * axis.Y - axis.Z * s, oc * axis.Z * axis.X + axis.Y * s,
                oc * axis.X * axis.Y + axis.Z * s, oc * axis.Y * axis.Y + c, oc * axis.Y * axis.Z - axis.X * s,
                oc * axis.Z * axis.X - axis.Y * s, oc * axis.Y * axis.Z + axis.X * s, oc * axis.Z * axis.Z + c
            );
        }*/

        public IComponent Clone() => new Transform
        {
            position = position,
            rotation = rotation,
            scaling = scaling,
        };
    }
}
