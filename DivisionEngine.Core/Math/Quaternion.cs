namespace DivisionEngine.Math
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

        public static float Dot(this float4 q, float4 p) => q.X * p.X + q.Y * p.Y + q.Z * p.Z + q.W * p.W;
        public static float4 Normalize(this float4 q)
        {
            float length = (float)Math.Sqrt(Dot(q, q));
            if (length == 0) return new float4(0, 0, 0, 1);
            return new float4(q.X / length, q.Y / length, q.Z / length, q.W / length);
        }
    }
}
