namespace DivisionEngine.MathLib
{
    /// <summary>
    /// Mathematical utilities for Division Engine.
    /// </summary>
    public static class Math
    {
        // Constants

        /// <summary>
        /// Represents the mathematical constant π (pi), the ratio of a circle's circumference to its diameter.
        /// </summary>
        /// <remarks>The value of this constant is approximately 3.14159. It is provided as a
        /// single-precision floating-point number.</remarks>
        public const float PI = (float)System.Math.PI;
        /// <summary>
        /// Represents the mathematical constant e (Euler's number), approximately equal to 2.718.
        /// </summary>
        /// <remarks>This constant is defined as a single-precision floating-point value. It is commonly
        /// used as the base of natural logarithms and in exponential calculations.</remarks>
        public const float E = (float)System.Math.E;
        /// <summary>
        /// Represents the mathematical constant τ (Tau), which is equal to 2π.
        /// </summary>
        /// <remarks>Tau is commonly used in mathematics and physics as an alternative to 2π, particularly
        /// in contexts involving circles and periodic functions.</remarks>
        public const float Tau = 2 * PI; // Tau is 2*Pi
        /// <summary>
        /// Represents the factor used to convert degrees to radians.
        /// </summary>
        /// <remarks>Multiply an angle in degrees by this constant to convert it to radians. For example,
        /// to convert 90 degrees to radians, calculate <c>90 * Deg2Rad</c>.</remarks>
        public const float Deg2Rad = PI / 180f;
        /// <summary>
        /// Represents the conversion factor from radians to degrees.
        /// </summary>
        /// <remarks>Multiply a value in radians by this constant to convert it to degrees. The value is
        /// equivalent to 180 divided by π.</remarks>
        public const float Rad2Deg = 180f / PI;
        /// <summary>
        /// Represents positive infinity for floating-point numbers.
        /// </summary>
        public const float Infinity = float.PositiveInfinity;
        /// <summary>
        /// Represents negative infinity for floating-point numbers.
        /// </summary>
        public const float NegativeInfinity = float.NegativeInfinity;

        // Basic Math Functions

        /// <summary>
        /// Returns the absolute value of a number.
        /// </summary>
        /// <param name="value">The value to find the absolute of</param>
        /// <returns>The absolute of <paramref name="value"/></returns>
        public static float Abs(float value) => System.Math.Abs(value);
        /// <summary>
        /// Computes the sine of the specified <paramref name="angle"/>.
        /// </summary>
        /// <param name="angle">The angle to compute the sine of</param>
        /// <returns>The sine of <paramref name="angle"/></returns>
        public static float Sin(float angle) => (float)System.Math.Sin(angle);
        /// <summary>
        /// Computes the cosine of the specified <paramref name="angle"/>.
        /// </summary>
        /// <param name="angle">The angle to compute the cosine of</param>
        /// <returns>The cosine of <paramref name="angle"/></returns>
        public static float Cos(float angle) => (float)System.Math.Cos(angle);
        /// <summary>
        /// Computes the tangent of the specified <paramref name="angle"/>.
        /// </summary>
        /// <param name="angle">The angle to compute the tangent of</param>
        /// <returns>The tangent of <paramref name="angle"/></returns>
        public static float Tan(float angle) => (float)System.Math.Tan(angle);
        /// <summary>
        /// Computes the arc-sine of the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The angle to compute the arc-sine of</param>
        /// <returns>The arc-sine of <paramref name="value"/></returns>
        public static float Asin(float value) => (float)System.Math.Asin(value);
        /// <summary>
        /// Computes the arc-cosine of the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The angle to compute the arc-cosine of</param>
        /// <returns>The arc-cosine of <paramref name="value"/></returns>
        public static float Acos(float value) => (float)System.Math.Acos(value);
        /// <summary>
        /// Computes the arc-tangent of the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The angle to compute the arc-tangent of</param>
        /// <returns>The arc-tangent of <paramref name="value"/></returns>
        public static float Atan(float value) => (float)System.Math.Atan(value);
        /// <summary>
        /// Computes the angle (in radians) whose tangent is the quotient of two specified numbers, y and x.
        /// </summary>
        /// <returns>The angle in radius</returns>
        public static float Atan2(float y, float x) => (float)System.Math.Atan2(y, x);
        public static float Pow(float x, float y) => (float)System.Math.Pow(x, y);
        public static float Exp(float x) => (float)System.Math.Exp(x);
        public static float Log(float x) => (float)System.Math.Log(x);
        public static float Log(float x, float newBase) => (float)System.Math.Log(x, newBase);
        public static float Log10(float x) => (float)System.Math.Log10(x);
        public static float Floor(float x) => (float)System.Math.Floor(x);
        public static int FloorToInt(float x) => (int)System.Math.Floor(x);
        public static float Ceiling(float x) => (float)System.Math.Ceiling(x);
        public static int CeilingToInt(float x) => (int)System.Math.Ceiling(x);
        public static float Round(float x) => (float)System.Math.Round(x);
        public static int RoundToInt(float x) => (int)System.Math.Round(x);
        public static int Sign(float x) => System.Math.Sign(x);
        public static int Sign(int x) => System.Math.Sign(x);
        public static float Max(float a, float b) => (a > b) ? a : b;
        public static int Max(int a, int b) => (a > b) ? a : b;
        public static float Min(float a, float b) => (a < b) ? a : b;
        public static int Min(int a, int b) => (a < b) ? a : b;

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static int Lerp(int a, int b, float t) => (int)(a + (b - a) * Clamp01(t));
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static int LerpUnclamped(int a, int b, float t) => (int)(a + (b - a) * t);

        public static float Sqrt(float x) => (float)System.Math.Sqrt(x);

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        /// <returns>The value clamped to the range defined by <paramref name="min"/> and <paramref name="max"/>. If <paramref
        /// name="value"/> is less than <paramref name="min"/>, <paramref name="min"/> is returned. If <paramref
        /// name="value"/> is greater than <paramref name="max"/>, <paramref name="max"/> is returned. Otherwise,
        /// <paramref name="value"/> is returned.</returns>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        
        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive lower bound of the range.</param>
        /// <param name="max">The inclusive upper bound of the range.</param>
        /// <returns>The value clamped to the range defined by <paramref name="min"/> and <paramref name="max"/>. If <paramref
        /// name="value"/> is less than <paramref name="min"/>, <paramref name="min"/> is returned. If <paramref
        /// name="value"/> is greater than <paramref name="max"/>, <paramref name="max"/> is returned. Otherwise,
        /// <paramref name="value"/> is returned.</returns>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Clamps a value to the range [0, 1].
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The clamped value, which will be 0 if <paramref name="value"/> is less than 0, 1 if <paramref
        /// name="value"/> is greater than 1, or the original value if it is within the range [0, 1].</returns>
        public static float Clamp01(float value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        /// <summary>
        /// Clamps a value to the range [0, 1].
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <returns>The clamped value, which will be 0 if <paramref name="value"/> is less than 0, 1 if <paramref
        /// name="value"/> is greater than 1, or the original value if it is within the range [0, 1].</returns>
        public static int Clamp01(int value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static int Remap(int value, int from1, int to1, int from2, int to2)
        {
            return (int)((value - from1) / (float)(to1 - from1) * (to2 - from2) + from2);
        }

        public static float WrapAngle(float angle)
        {
            angle = angle % 360;
            if (angle < 0) angle += 360;
            return angle;
        }

        public static float WrapAngleRadians(float angle)
        {
            angle = angle % Tau;
            if (angle < 0) angle += Tau;
            return angle;
        }

        public static float3 QuaternionToEuler(this float4 q)
        {
            // Roll (x-axis rotation)
            float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            float roll = Atan2(sinr_cosp, cosr_cosp);

            // Pitch (y-axis rotation)
            float sinp = 2 * (q.W * q.Y - q.Z * q.X);
            float pitch;
            if (Abs(sinp) >= 1)
                pitch = PI / 2 * Sign(sinp); // use 90 degrees if out of range
            else
                pitch = Asin(sinp);

            // Yaw (z-axis rotation)
            float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            float yaw = Atan2(siny_cosp, cosy_cosp);
            return new float3(roll, pitch, yaw);
        }

        public static float4 EulerToQuaternion(this float3 euler)
        {
            // Abbreviations for the various angular functions
            double cy = Cos(euler.Z * 0.5f);
            double sy = Sin(euler.Z * 0.5f);
            double cr = Cos(euler.X * 0.5f);
            double sr = Sin(euler.X * 0.5f);
            double cp = Cos(euler.Y * 0.5f);
            double sp = Sin(euler.Y * 0.5f);
            float w = (float)(cy * cr * cp + sy * sr * sp);
            float x = (float)(cy * sr * cp - sy * cr * sp);
            float y = (float)(cy * cr * sp + sy * sr * cp);
            float z = (float)(sy * cr * cp - cy * sr * sp);
            return new float4(x, y, z, w);
        }
    }
}
