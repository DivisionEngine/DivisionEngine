namespace DivisionEngine.MathLib
{
    /// <summary>
    /// Extension methods for quaternion vector operations on float4 objects.
    /// </summary>
    /// <remarks>This class is still untested, and therefore cannot be used in production yet</remarks>
    public static class Quaternion
    {
        /// <summary>
        /// Identity quaternion => float4(0, 0, 0, 1) .
        /// </summary>
        public static readonly float4 Identity = new(0, 0, 0, 1);

        /// <summary>
        /// Calculates the dot product between two float4 quaternions.
        /// </summary>
        /// <param name="q">Left quaternion</param>
        /// <param name="p">Right quaternion</param>
        /// <returns>Dot product of two float4 quaternions</returns>
        public static float Dot(this float4 q, float4 p) => q.X * p.X + q.Y * p.Y + q.Z * p.Z + q.W * p.W;

        /// <summary>
        /// Normalizes a quaternion vector.
        /// </summary>
        /// <param name="q">Quaternion to normalize</param>
        /// <returns>Normalized quaternion</returns>
        public static float4 Normalize(this float4 q)
        {
            float length = (float)Math.Sqrt(Dot(q, q));
            if (length == 0) return new float4(0, 0, 0, 1);
            return new float4(q.X / length, q.Y / length, q.Z / length, q.W / length);
        }

        /// <summary>
        /// Converts a float4 quaternion to a System.Numerics quaternion.
        /// </summary>
        /// <param name="quaternion">Float4 vector to convert</param>
        /// <returns>System quaternion object</returns>
        public static System.Numerics.Quaternion Float4ToQuaternion(this float4 quaternion) =>
            new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);

        /// <summary>
        /// Converts a system quaternion to a float4 quaternion.
        /// </summary>
        /// <param name="quaternion">System quaternion to convert</param>
        /// <returns>Float4 quaternion vector converted</returns>
        public static float4 QuaternionToFloat4(this System.Numerics.Quaternion quaternion) =>
            new float4(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }
}
