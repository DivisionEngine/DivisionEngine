using System.Numerics;

namespace DivisionEngine.Math
{
    /// <summary>
    /// Handles common matrix operations and provides predefined matrices.
    /// </summary>
    public static class Matrix
    {
        /// <summary>
        /// Represents a 2x2 matrix with all elements set to zero.
        /// </summary>
        public static float2x2 Zero2x2 => new float2x2(
            0, 0,
            0, 0
        );

        /// <summary>
        /// Represents a 3x3 matrix with all elements set to zero.
        /// </summary>
        public static float3x3 Zero3x3 => new float3x3(
            0, 0, 0,
            0, 0, 0,
            0, 0, 0
        );

        /// <summary>
        /// Represents a 4x4 matrix with all elements set to zero.
        /// </summary>
        public static float4x4 Zero4x4 => new float4x4(
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        );

        /// <summary>
        /// Represents a 2x2 identity matrix.
        /// </summary>
        public static float2x2 Identity2x2 => new float2x2(
            1, 0,
            0, 1
        );

        /// <summary>
        /// Represents a 3x3 identity matrix.
        /// </summary>
        public static float3x3 Identity3x3 => new float3x3(
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        );

        /// <summary>
        /// Represents a 4x4 identity matrix.
        /// </summary>
        public static float4x4 Identity4x4 => new float4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        /// <summary>
        /// Calculates the determinant of a 2x2 matrix.
        /// </summary>
        /// <param name="matrix">Matrix to calculate the determinant of</param>
        /// <returns>Determinant of <param name="matrix"></returns>
        public static float Determinant(this float2x2 matrix)
        {
            return matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
        }

        /// <summary>
        /// Calculates the determinant of a 3x3 matrix.
        /// </summary>
        /// <param name="matrix">Matrix to calculate the determinant of</param>
        /// <returns>Determinant of <param name="matrix"></returns>
        public static float Determinant(this float3x3 matrix)
        {
            return matrix.M11 * (matrix.M22 * matrix.M33 - matrix.M23 * matrix.M32) -
                   matrix.M12 * (matrix.M21 * matrix.M33 - matrix.M23 * matrix.M31) +
                   matrix.M13 * (matrix.M21 * matrix.M32 - matrix.M22 * matrix.M31);
        }

        /// <summary>
        /// Calculates the determinant of a 4x4 matrix.
        /// </summary>
        /// <param name="matrix">Matrix to calculate the determinant of</param>
        /// <returns>Determinant of <param name="matrix"></returns>
        public static float Determinant(this float4x4 matrix)
        {
            return matrix.M11 * (matrix.M22 * (matrix.M33 * matrix.M44 - matrix.M34 * matrix.M43) -
                                 matrix.M23 * (matrix.M32 * matrix.M44 - matrix.M34 * matrix.M42) +
                                 matrix.M24 * (matrix.M32 * matrix.M43 - matrix.M33 * matrix.M42)) -
                   matrix.M12 * (matrix.M21 * (matrix.M33 * matrix.M44 - matrix.M34 * matrix.M43) -
                                 matrix.M23 * (matrix.M31 * matrix.M44 - matrix.M34 * matrix.M41) +
                                 matrix.M24 * (matrix.M31 * matrix.M43 - matrix.M33 * matrix.M41)) +
                   matrix.M13 * (matrix.M21 * (matrix.M32 * matrix.M44 - matrix.M34 * matrix.M42) -
                                 matrix.M22 * (matrix.M31 * matrix.M44 - matrix.M34 * matrix.M41) +
                                 matrix.M24 * (matrix.M31 * matrix.M42 - matrix.M32 * matrix.M41)) -
                   matrix.M14 * (matrix.M21 * (matrix.M32 * matrix.M43 - matrix.M33 * matrix.M42) -
                                 matrix.M22 * (matrix.M31 * matrix.M43 - matrix.M33 * matrix.M41) +
                                 matrix.M23 * (matrix.M31 * matrix.M42 - matrix.M32 * matrix.M41));
        }

        /// <summary>
        /// Converts a float4x4 matrix to a System.Numerics.Matrix4x4.
        /// </summary>
        /// <param name="matrix">Matrix to convert</param>
        /// <returns>Converted matrix</returns>
        internal static Matrix4x4 Float4x4ToMatrix4x4(this float4x4 matrix)
        {
            return new Matrix4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }

        /// <summary>
        /// Converts a System.Numerics.Matrix4x4 to a float4x4.
        /// </summary>
        /// <param name="matrix">Matrix to convert</param>
        /// <returns>Converted matrix</returns>
        internal static float4x4 Matrix4x4ToFloat4x4(this Matrix4x4 matrix)
        {
            return new float4x4(
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            );
        }

        internal static System.Numerics.Quaternion Float4ToQuaternion(this float4 quaternion)
        {
            return new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        internal static float4 QuaternionToFloat4(this System.Numerics.Quaternion quaternion)
        {
            return new float4(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }

        internal static Vector3 Float3ToVector3(this float3 vector)
        {
            return new System.Numerics.Vector3(vector.X, vector.Y, vector.Z);
        }

        internal static float3 Vector3ToFloat3(this Vector3 vector)
        {
            return new float3(vector.X, vector.Y, vector.Z);
        }

        /// <summary>
        /// Computes the inverse of the specified 2x2 matrix.
        /// </summary>
        /// <remarks>The method calculates the inverse of the given matrix using a mathematical approach.
        /// If the determinant of the matrix is zero, indicating that the matrix is singular, the method returns the
        /// identity matrix instead.</remarks>
        /// <param name="matrix">The <see cref="float2x2"/> matrix to invert.</param>
        /// <returns>A new <see cref="float2x2"/> representing the inverse of the input matrix. If the matrix is singular
        /// (non-invertible), the returned matrix will be the identity matrix.</returns>
        public static float2x2 Inverse(this float2x2 matrix)
        {
            float det = matrix.Determinant();
            if (det == 0)
                return Identity2x2; // Return identity if not invertible
            float invDet = 1.0f / det;
            return new float2x2(
                matrix.M22 * invDet, -matrix.M12 * invDet,
                -matrix.M21 * invDet, matrix.M11 * invDet
            );
        }

        /// <summary>
        /// Computes the inverse of the specified 4x4 matrix.
        /// </summary>
        /// <remarks>The method calculates the inverse of the given matrix using a mathematical approach.
        /// If the determinant of the matrix is zero, indicating that the matrix is singular, the method returns the
        /// identity matrix instead.</remarks>
        /// <param name="matrix">The <see cref="float4x4"/> matrix to invert.</param>
        /// <returns>A new <see cref="float4x4"/> representing the inverse of the input matrix. If the matrix is singular
        /// (non-invertible), the returned matrix will be the identity matrix.</returns>
        public static float4x4 Inverse(this float4x4 matrix)
        {
            if (Matrix4x4.Invert(matrix.Float4x4ToMatrix4x4(), out Matrix4x4 m))
                return m.Matrix4x4ToFloat4x4();
            return Identity4x4;
        }

        /// <summary>
        /// Transposes the specified 2x2 matrix.
        /// </summary>
        /// <param name="matrix">Matrix to transpose</param>
        /// <returns>Transposed matrix</returns>
        public static float2x2 Transpose(this float2x2 matrix)
        {
            return new float2x2(
                matrix.M11, matrix.M21,
                matrix.M12, matrix.M22
            );
        }

        /// <summary>
        /// Transposes the specified 3x3 matrix.
        /// </summary>
        /// <param name="matrix">Matrix to transpose</param>
        /// <returns>Transposed matrix</returns>
        public static float3x3 Transpose(this float3x3 matrix)
        {
            return new float3x3(
                matrix.M11, matrix.M21, matrix.M31,
                matrix.M12, matrix.M22, matrix.M32,
                matrix.M13, matrix.M23, matrix.M33
            );
        }

        /// <summary>
        /// Transposes the specified 4x4 matrix.
        /// </summary>
        /// <param name="matrix">Matrix to transpose</param>
        /// <returns>Transposed matrix</returns>
        public static float4x4 Transpose(this float4x4 matrix)
        {
            return new float4x4(
                matrix.M11, matrix.M21, matrix.M31, matrix.M41,
                matrix.M12, matrix.M22, matrix.M32, matrix.M42,
                matrix.M13, matrix.M23, matrix.M33, matrix.M43,
                matrix.M14, matrix.M24, matrix.M34, matrix.M44
            );
        }

        public static float4x4 CreateMatrix4x4FromQuaternion(this float4 quaternion)
        {
            return Matrix4x4.CreateFromQuaternion(quaternion.Float4ToQuaternion()).Matrix4x4ToFloat4x4();
        }

        public static float4x4 CreateMatrix4x4FromTranslation(this float3 translation)
        {
            return Matrix4x4.CreateTranslation(translation.Float3ToVector3()).Matrix4x4ToFloat4x4();
        }

        // Test this method before using it in production!
        // The algorithm is based on the following StackOverflow post:
        // The code is commented out because it currently is experimental and untested.
        // https://stackoverflow.com/questions/1148309/inverting-a-4x4-matrix
        /*public static float4x4 Invert(this float4x4 matrix)
        {
            float[] m = [
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            ];
            double[] inv = [16];
            double det;
            int i;

            inv[0] = m[5] * m[10] * m[15] -
                        m[5] * m[11] * m[14] -
                        m[9] * m[6] * m[15] +
                        m[9] * m[7] * m[14] +
                        m[13] * m[6] * m[11] -
                        m[13] * m[7] * m[10];

            inv[4] = -m[4] * m[10] * m[15] +
                        m[4] * m[11] * m[14] +
                        m[8] * m[6] * m[15] -
                        m[8] * m[7] * m[14] -
                        m[12] * m[6] * m[11] +
                        m[12] * m[7] * m[10];

            inv[8] = m[4] * m[9] * m[15] -
                        m[4] * m[11] * m[13] -
                        m[8] * m[5] * m[15] +
                        m[8] * m[7] * m[13] +
                        m[12] * m[5] * m[11] -
                        m[12] * m[7] * m[9];

            inv[12] = -m[4] * m[9] * m[14] +
                        m[4] * m[10] * m[13] +
                        m[8] * m[5] * m[14] -
                        m[8] * m[6] * m[13] -
                        m[12] * m[5] * m[10] +
                        m[12] * m[6] * m[9];

            inv[1] = -m[1] * m[10] * m[15] +
                        m[1] * m[11] * m[14] +
                        m[9] * m[2] * m[15] -
                        m[9] * m[3] * m[14] -
                        m[13] * m[2] * m[11] +
                        m[13] * m[3] * m[10];

            inv[5] = m[0] * m[10] * m[15] -
                        m[0] * m[11] * m[14] -
                        m[8] * m[2] * m[15] +
                        m[8] * m[3] * m[14] +
                        m[12] * m[2] * m[11] -
                        m[12] * m[3] * m[10];

            inv[9] = -m[0] * m[9] * m[15] +
                        m[0] * m[11] * m[13] +
                        m[8] * m[1] * m[15] -
                        m[8] * m[3] * m[13] -
                        m[12] * m[1] * m[11] +
                        m[12] * m[3] * m[9];

            inv[13] = m[0] * m[9] * m[14] -
                        m[0] * m[10] * m[13] -
                        m[8] * m[1] * m[14] +
                        m[8] * m[2] * m[13] +
                        m[12] * m[1] * m[10] -
                        m[12] * m[2] * m[9];

            inv[2] = m[1] * m[6] * m[15] -
                        m[1] * m[7] * m[14] -
                        m[5] * m[2] * m[15] +
                        m[5] * m[3] * m[14] +
                        m[13] * m[2] * m[7] -
                        m[13] * m[3] * m[6];

            inv[6] = -m[0] * m[6] * m[15] +
                        m[0] * m[7] * m[14] +
                        m[4] * m[2] * m[15] -
                        m[4] * m[3] * m[14] -
                        m[12] * m[2] * m[7] +
                        m[12] * m[3] * m[6];

            inv[10] = m[0] * m[5] * m[15] -
                        m[0] * m[7] * m[13] -
                        m[4] * m[1] * m[15] +
                        m[4] * m[3] * m[13] +
                        m[12] * m[1] * m[7] -
                        m[12] * m[3] * m[5];

            inv[14] = -m[0] * m[5] * m[14] +
                        m[0] * m[6] * m[13] +
                        m[4] * m[1] * m[14] -
                        m[4] * m[2] * m[13] -
                        m[12] * m[1] * m[6] +
                        m[12] * m[2] * m[5];

            inv[3] = -m[1] * m[6] * m[11] +
                        m[1] * m[7] * m[10] +
                        m[5] * m[2] * m[11] -
                        m[5] * m[3] * m[10] -
                        m[9] * m[2] * m[7] +
                        m[9] * m[3] * m[6];

            inv[7] = m[0] * m[6] * m[11] -
                        m[0] * m[7] * m[10] -
                        m[4] * m[2] * m[11] +
                        m[4] * m[3] * m[10] +
                        m[8] * m[2] * m[7] -
                        m[8] * m[3] * m[6];

            inv[11] = -m[0] * m[5] * m[11] +
                        m[0] * m[7] * m[9] +
                        m[4] * m[1] * m[11] -
                        m[4] * m[3] * m[9] -
                        m[8] * m[1] * m[7] +
                        m[8] * m[3] * m[5];

            inv[15] = m[0] * m[5] * m[10] -
                        m[0] * m[6] * m[9] -
                        m[4] * m[1] * m[10] +
                        m[4] * m[2] * m[9] +
                        m[8] * m[1] * m[6] -
                        m[8] * m[2] * m[5];

            det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];

            if (det == 0)
                return Matrix.Zero4x4;

            det = 1.0 / det;

            float[] invOut = [16];
            for (i = 0; i < 16; i++)
                invOut[i] = (float)(inv[i] * det);

            return new float4x4(invOut[0], invOut[1], invOut[2], invOut[3],
                                  invOut[4], invOut[5], invOut[6], invOut[7],
                                  invOut[8], invOut[9], invOut[10], invOut[11],
                                  invOut[12], invOut[13], invOut[14], invOut[15]);
        }*/
    }
}
