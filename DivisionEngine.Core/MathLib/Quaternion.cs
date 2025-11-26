using System.Numerics;

namespace DivisionEngine.MathLib
{
    /// <summary>
    /// Extension methods for quaternion vector operations on float4 objects.
    /// </summary>
    public static class Quaternion
    {
        /// <summary>
        /// Identity quaternion => float4(0, 0, 0, 1).
        /// </summary>
        public static readonly float4 Identity = new(0, 0, 0, 1);

        /// <summary>
        /// Normalizes a quaternion vector.
        /// </summary>
        /// <param name="q">Quaternion to normalize</param>
        /// <returns>Normalized quaternion</returns>
        public static float4 Normalize(this float4 q)
        {
            float length = Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
            if (length < 0.0001f) return Identity;
            return new float4(q.X / length, q.Y / length, q.Z / length, q.W / length);
        }

        /// <summary>
        /// Create a quaternion rotation from an axis and an angle (in radians).
        /// </summary>
        /// <param name="axis">Axis to rotate around</param>
        /// <param name="angle">Angle in radias</param>
        /// <returns>Quaternion rotation around axis</returns>
        public static float4 CreateFromAxisAngle(float3 axis, float angle) =>
            System.Numerics.Quaternion.CreateFromAxisAngle(axis.ToVector3(), angle).ToFloat4();

        /// <summary>
        /// Creates a quaternion rotation from a float4x4 rotation matrix.
        /// </summary>
        /// <param name="matrix">Rotation matrix</param>
        /// <returns>Quaternion rotation from matrix</returns>
        public static float4 CreateFromRotationMatrix(float4x4 matrix) =>
            System.Numerics.Quaternion.CreateFromRotationMatrix(matrix.ToMatrix4x4()).ToFloat4();

        /// <summary>
        /// Creates a quaternion rotation from yaw, pitch, and roll values.
        /// </summary>
        /// <param name="yaw">Yaw value</param>
        /// <param name="pitch">Pitch value</param>
        /// <param name="roll">Roll value</param>
        /// <returns>Quaternion from yaw, pitch, roll</returns>
        public static float4 CreateFromYawPitchRoll(float yaw, float pitch, float roll) =>
            System.Numerics.Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll).ToFloat4();

        /// <summary>
        /// Inverts this quaternion values.
        /// </summary>
        /// <param name="quaternion">Quaternion to invert</param>
        /// <returns>Inverted quaternion</returns>
        public static float4 InvertQuaternion(this float4 quaternion) =>
            System.Numerics.Quaternion.Inverse(quaternion.ToQuaternion()).ToFloat4();

        /// <summary>
        /// Linear interpolation between two quaternions by amount t.
        /// </summary>
        /// <param name="a">Interpolation origin</param>
        /// <param name="b">Interpolation target</param>
        /// <param name="t">Amount to interpolate</param>
        /// <returns>Interpolated quaternion rotation</returns>
        public static float4 Lerp(float4 a, float4 b, float t) =>
            System.Numerics.Quaternion.Lerp(a.ToQuaternion(), b.ToQuaternion(), t).ToFloat4();

        /// <summary>
        /// Adds two quaternions together.
        /// </summary>
        /// <param name="a">Quaternion a</param>
        /// <param name="b">Quaternion b</param>
        /// <returns>Addition of quaterions</returns>
        public static float4 Add(float4 a, float4 b) => (a.ToQuaternion() + b.ToQuaternion()).ToFloat4();

        /// <summary>
        /// Subtracts two quaternions together.
        /// </summary>
        /// <param name="a">Quaternion a</param>
        /// <param name="b">Quaternion b</param>
        /// <returns>Subtraction of quaterions</returns>
        public static float4 Subtract(float4 a, float4 b) => (a.ToQuaternion() - b.ToQuaternion()).ToFloat4();

        /// <summary>
        /// Multiplies two quaternions together.
        /// </summary>
        /// <param name="a">Quaternion a</param>
        /// <param name="b">Quaternion b</param>
        /// <returns>Product of quaterions</returns>
        public static float4 Multiply(float4 a, float4 b) => (a.ToQuaternion() * b.ToQuaternion()).ToFloat4();

        /// <summary>
        /// Divides two quaternions together.
        /// </summary>
        /// <param name="numerator">Quaternion a</param>
        /// <param name="denominator">Quaternion b</param>
        /// <returns>Division of quaterions</returns>
        public static float4 Divide(float4 numerator, float4 denominator) =>
            (numerator.ToQuaternion() / denominator.ToQuaternion()).ToFloat4();

        /// <summary>
        /// Converts a float4 quaternion to a System.Numerics quaternion.
        /// </summary>
        /// <param name="quaternion">Float4 vector to convert</param>
        /// <returns>System quaternion object</returns>
        public static System.Numerics.Quaternion ToQuaternion(this float4 quaternion) =>
            new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

        /// <summary>
        /// Converts a system quaternion to a float4 quaternion.
        /// </summary>
        /// <param name="quaternion">System quaternion to convert</param>
        /// <returns>Float4 quaternion vector converted</returns>
        public static float4 ToFloat4(this System.Numerics.Quaternion quaternion) =>
            new float4(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

        /// <summary>
        /// Rotates a float3 vector by a quaternion rotation.
        /// </summary>
        /// <param name="quaternion">Quaternion rotation</param>
        /// <param name="vector">Vector to rotate</param>
        /// <returns>Float3 rotated vector</returns>
        public static float3 RotateVector(this float4 quaternion, float3 vector)
        {
            float3 qVec = new float3(quaternion.X, quaternion.Y, quaternion.Z);
            float s = quaternion.W;

            // Function form of: v' = v + 2.0 * cross(u, cross(u, v) + s * v)
            float3 crossA = qVec.Cross(vector).Add(vector.Multiply(s));
            float3 crossB = qVec.Cross(crossA).Multiply(2f);
            return vector.Add(crossB);
        }
    }
}
