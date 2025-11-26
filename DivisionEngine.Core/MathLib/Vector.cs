using System.Numerics;

namespace DivisionEngine.MathLib
{
    /// <summary>
    /// Extension methods for float3 vector operations.
    /// </summary>
    public static class Vector
    {
        // Add vectors
        public static float2 Add(this float2 a, float2 b) => new float2(a.X + b.X, a.Y + b.Y);
        public static float3 Add(this float3 a, float3 b) => new float3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static float4 Add(this float4 a, float4 b) => new float4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

        public static float2 Add(this float2 a, float b) => new float2(a.X + b, a.Y + b);
        public static float3 Add(this float3 a, float b) => new float3(a.X + b, a.Y + b, a.Z + b);
        public static float4 Add(this float4 a, float b) => new float4(a.X + b, a.Y + b, a.Z + b, a.W + b);

        // Subtract vectors
        public static float2 Subtract(this float2 a, float2 b) => new float2(a.X - b.X, a.Y - b.Y);
        public static float3 Subtract(this float3 a, float3 b) => new float3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static float4 Subtract(this float4 a, float4 b) => new float4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

        public static float2 Subtract(this float2 a, float b) => new float2(a.X - b, a.Y - b);
        public static float3 Subtract(this float3 a, float b) => new float3(a.X - b, a.Y - b, a.Z - b);
        public static float4 Subtract(this float4 a, float b) => new float4(a.X - b, a.Y - b, a.Z - b, a.W - b);

        // Multiply vectors
        public static float2 Multiply(this float2 a, float2 b) => new float2(a.X * b.X, a.Y * b.Y);
        public static float3 Multiply(this float3 a, float3 b) => new float3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        public static float4 Multiply(this float4 a, float4 b) => new float4(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);

        public static float2 Multiply(this float2 a, float b) => new float2(a.X * b, a.Y * b);
        public static float3 Multiply(this float3 a, float b) => new float3(a.X * b, a.Y * b, a.Z * b);
        public static float4 Multiply(this float4 a, float b) => new float4(a.X * b, a.Y * b, a.Z * b, a.W * b);

        // Divide vectors
        public static float2 Divide(this float2 a, float2 b) => new float2(a.X / b.X, a.Y / b.Y);
        public static float3 Divide(this float3 a, float3 b) => new float3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        public static float4 Divide(this float4 a, float4 b) => new float4(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);

        public static float2 Divide(this float2 a, float b) => new float2(a.X / b, a.Y / b);
        public static float3 Divide(this float3 a, float b) => new float3(a.X / b, a.Y / b, a.Z / b);
        public static float4 Divide(this float4 a, float b) => new float4(a.X / b, a.Y / b, a.Z / b, a.W / b);

        /// <summary>
        /// Calculates the dot product of two float2 vectors.
        /// </summary>
        /// <param name="a">Vector a</param>
        /// <param name="b">Vector b</param>
        /// <returns>Dot product of vectors a and b</returns>
        public static float Dot(this float2 a, float2 b) => a.X * b.X + a.Y * b.Y;

        /// <summary>
        /// Calculates the dot product of two float3 vectors.
        /// </summary>
        /// <param name="a">Vector a</param>
        /// <param name="b">Vector b</param>
        /// <returns>Dot product of vectors a and b</returns>
        public static float Dot(this float3 a, float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        /// <summary>
        /// Calculates the dot product between two float4 vectors.
        /// </summary>
        /// <param name="q">Left vectors</param>
        /// <param name="p">Right vectors</param>
        /// <returns>Dot product of two float4 vectors</returns>
        public static float Dot(this float4 q, float4 p) => q.X * p.X + q.Y * p.Y + q.Z * p.Z + q.W * p.W;

        /// <summary>
        /// Computes the length (magnitude) of a the vector v.
        /// </summary>
        /// <param name="v">Magnitude vector</param>
        /// <returns>Magnitude of vector v</returns>
        public static float Length(this float2 v) => (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);

        /// <summary>
        /// Computes the length (magnitude) of a the vector v.
        /// </summary>
        /// <param name="v">Magnitude vector</param>
        /// <returns>Magnitude of vector v</returns>
        public static float Length(this float3 v) => (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);

        /// <summary>
        /// Computes distance between two float2 vectors.
        /// </summary>
        /// <param name="a">Vector a</param>
        /// <param name="b">Vector b</param>
        /// <returns>Distance between vectors a and b</returns>
        public static float Distance(this float2 a, float2 b) => Length(b - a);

        /// <summary>
        /// Computes distance between two float3 vectors.
        /// </summary>
        /// <param name="a">Vector a</param>
        /// <param name="b">Vector b</param>
        /// <returns>Distance between vectors a and b</returns>
        public static float Distance(this float3 a, float3 b) => Length(b - a);

        /// <summary>
        /// Computes the cross product between two float3 vectors.
        /// </summary>
        /// <param name="a">Left vector</param>
        /// <param name="b">Right vector</param>
        /// <returns>Cross product of vectors</returns>
        public static float3 Cross(this float3 a, float3 b) => new float3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );

        /// <summary>
        /// Normalizes a float2 vector value.
        /// </summary>
        /// <param name="v">Vector to normalize</param>
        /// <returns>Normalized vector value</returns>
        public static float2 Normalize(this float2 v) => Vector2.Normalize(v.ToVector2()).ToFloat2();

        /// <summary>
        /// Normalizes a float3 vector value.
        /// </summary>
        /// <param name="v">Vector to normalize</param>
        /// <returns>Normalized vector value</returns>
        public static float3 Normalize(this float3 v) => Vector3.Normalize(v.ToVector3()).ToFloat3();

        /// <summary>
        /// Reflects a vector over a normal vector.
        /// </summary>
        /// <param name="I">Vector to reflect</param>
        /// <param name="N">Normal vector to reflect over</param>
        /// <returns>Reflection vector</returns>
        public static float3 Reflect(float3 I, float3 N) => Vector3.Reflect(I.ToVector3(), N.ToVector3()).ToFloat3();

        public static float3 Refract(float3 I, float3 N, float eta)
        {
            float cosi = Math.Clamp(Dot(I, N), -1, 1);
            float etai = 1, etat = eta;
            if (cosi < 0) cosi = -cosi;
            else
            {
                (etat, etai) = (etai, etat);
                N = -N;
            }
            float etaRatio = etai / etat;
            float k = 1 - etaRatio * etaRatio * (1 - cosi * cosi);
            if (k < 0) return new float3(0, 0, 0);
            else return etaRatio * I + (etaRatio * cosi - (float)Math.Sqrt(k)) * N;
        }

        public static float3 Lerp(float3 a, float3 b, float t) => Vector3.Lerp(a.ToVector3(), b.ToVector3(), t).ToFloat3();

        public static float3 Slerp(float3 a, float3 b, float t)
        {
            float dot = Dot(Normalize(a), Normalize(b));
            dot = Math.Clamp(dot, -1, 1);
            float theta = (float)Math.Acos(dot) * t;
            float3 relativeVec = Normalize(b - dot * a);
            return Normalize(a * (float)Math.Cos(theta) + relativeVec * (float)Math.Sin(theta));
        }

        /// <summary>
        /// Converts a float2 vector to a System.Numerics Vector2 value.
        /// </summary>
        /// <param name="vector">Vector to convert</param>
        /// <returns>Converted Vector2 value</returns>
        public static Vector2 ToVector2(this float2 vector) => new Vector2(vector.X, vector.Y);

        /// <summary>
        /// Converts a System Vector2 value to a float2 vector.
        /// </summary>
        /// <param name="vector">Vector to convert</param>
        /// <returns>Converted float2 vector value</returns>
        public static float2 ToFloat2(this Vector2 vector) => new float2(vector.X, vector.Y);

        /// <summary>
        /// Converts a float3 vector to a System.Numerics Vector3 value.
        /// </summary>
        /// <param name="vector">Vector to convert</param>
        /// <returns>Converted Vector3 value</returns>
        public static Vector3 ToVector3(this float3 vector) => new Vector3(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// Converts a System Vector3 value to a float3 vector.
        /// </summary>
        /// <param name="vector">Vector to convert</param>
        /// <returns>Converted float3 vector value</returns>
        public static float3 ToFloat3(this Vector3 vector) => new float3(vector.X, vector.Y, vector.Z);

        /// <summary>
        /// Converts a float4 vector to a System.Numerics Vector4 value.
        /// </summary>
        /// <param name="vector">Vector to convert</param>
        /// <returns>Converted Vector4 value</returns>
        public static Vector4 ToVector4(this float4 vector) => new Vector4(vector.X, vector.Y, vector.Z, vector.W);

        /// <summary>
        /// Converts a System Vector4 value to a float4 vector.
        /// </summary>
        /// <param name="vector">Vector to convert</param>
        /// <returns>Converted float4 vector value</returns>
        public static float4 ToFloat4(this Vector4 vector) => new float4(vector.X, vector.Y, vector.Z, vector.W);
    }
}
