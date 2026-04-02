//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;

namespace DivisionEngine.Rendering
{
    /// <summary>
    /// Simplex noise functions for 2D and 3D.
    /// Based on the original implementation by Ken Perlin and Stefan Gustavson.
    /// </summary>
    public static class SimplexNoise
    {
        // Skew and unskew factors
        private const float F2 = 0.5f * (1.7320508f - 1.0f); // (sqrt(3)-1)/2
        private const float G2 = (3.0f - 1.7320508f) / 6.0f; // (3-sqrt(3))/6
        private const float F3 = 1.0f / 3.0f;
        private const float G3 = 1.0f / 6.0f;

        /// <summary>
        /// Get gradient for 2D based on hash (no arrays)
        /// </summary>
        private static float2 Grad2(uint hash)
        {
            uint g = hash & 7;

            if (g == 0) return new float2(1, 1);
            if (g == 1) return new float2(-1, 1);
            if (g == 2) return new float2(1, -1);
            if (g == 3) return new float2(-1, -1);
            if (g == 4) return new float2(1, 0);
            if (g == 5) return new float2(-1, 0);
            if (g == 6) return new float2(0, 1);
            return new float2(0, -1);
        }

        /// <summary>
        /// Get gradient for 3D based on hash (no arrays)
        /// </summary>
        private static float3 Grad3(uint hash)
        {
            uint g = hash & 15;
            float3 grad = float3.Zero;

            // x component
            if (g < 8) grad.X = 1.0f;
            else grad.X = -1.0f;

            // y component
            if (g < 4) grad.Y = 1.0f;
            else if (g < 12) grad.Y = -1.0f;
            else grad.Y = 0.0f;

            // z component
            if ((g & 1) != 0) grad.Z = 1.0f;
            else if ((g & 2) != 0) grad.Z = -1.0f;
            else grad.Z = 0.0f;

            return grad;
        }

        /// <summary>
        /// Fast hash function for consistent pseudo-random values
        /// </summary>
        private static uint Hash(uint x)
        {
            x = (x ^ 61) ^ (x >> 16);
            x = x + (x << 3);
            x = x ^ (x >> 4);
            x = x * 0x27d4eb2d;
            x = x ^ (x >> 15);
            return x;
        }

        /// <summary>
        /// 2D hash from two integers
        /// </summary>
        private static uint Hash2(int2 p)
        {
            return Hash(Hash((uint)p.X) ^ Hash((uint)p.Y));
        }

        /// <summary>
        /// 3D hash from three integers
        /// </summary>
        private static uint Hash3(int3 p)
        {
            return Hash(Hash(Hash((uint)p.X) ^ Hash((uint)p.Y)) ^ Hash((uint)p.Z));
        }

        /// <summary>
        /// Falloff function - t^4 for smooth blending
        /// </summary>
        private static float Falloff(float t2)
        {
            if (t2 < 0) return 0;
            float t = t2 * t2;
            return t * t;
        }

        // ============================================================
        // 2D SIMPLEX NOISE
        // ============================================================

        /// <summary>
        /// 2D Simplex noise, returns value in range [0, 1]
        /// </summary>
        public static float Noise2D(float2 p)
        {
            // Step 1: Skew to triangular grid
            float s = (p.X + p.Y) * F2;
            float2 skew = p + s;

            // Step 2: Find cell coordinates
            int2 i = (int2)Hlsl.Floor(skew);
            float2 f = skew - i;

            // Step 3: Determine which triangle we're in (lower or upper)
            int2 ijk;
            if (f.X > f.Y)
                ijk = new int2(1, 0);
            else
                ijk = new int2(0, 1);

            // Step 4: Get vertices (unskewed)
            float t = (i.X + i.Y) * G2;
            float2 p0 = (float2)i - t;

            t = (i.X + ijk.X + i.Y + ijk.Y) * G2;
            float2 p1 = (float2)(i + ijk) - t;

            t = (i.X + 1 + i.Y + 1) * G2;
            float2 p2 = (float2)(i + 1) - t;

            // Step 5: Vectors from vertices to point
            float2 d0 = p - p0;
            float2 d1 = p - p1;
            float2 d2 = p - p2;

            // Step 6: Get gradients
            uint hash0 = Hash2(i);
            uint hash1 = Hash2(i + ijk);
            uint hash2 = Hash2(i + new int2(1, 1));

            float2 grad0 = Grad2(hash0 % 12);
            float2 grad1 = Grad2(hash1 % 12);
            float2 grad2 = Grad2(hash2 % 12);

            // Step 7: Calculate contributions
            float t0 = Falloff(0.6f - Hlsl.Dot(d0, d0));
            float t1 = Falloff(0.6f - Hlsl.Dot(d1, d1));
            float t2 = Falloff(0.6f - Hlsl.Dot(d2, d2));

            float n0 = t0 * Hlsl.Dot(grad0, d0);
            float n1 = t1 * Hlsl.Dot(grad1, d1);
            float n2 = t2 * Hlsl.Dot(grad2, d2);

            // Step 8: Sum and map to [0,1]
            return (n0 + n1 + n2) * 0.5f + 0.5f;
        }

        // ============================================================
        // 3D SIMPLEX NOISE
        // ============================================================

        /// <summary>
        /// 3D Simplex noise, returns value in range [0, 1]
        /// </summary>
        public static float Noise3D(float3 p)
        {
            // Step 1: Skew to tetrahedral grid
            float s = (p.X + p.Y + p.Z) * F3;
            float3 skew = p + s;

            // Step 2: Find cell coordinates
            int3 i = (int3)Hlsl.Floor(skew);
            float3 f = skew - i;

            // Step 3: Determine which tetrahedron we're in
            // Compare fractional parts to determine the ordering
            int3 ijk;
            if (f.X >= f.Y)
            {
                if (f.Y >= f.Z)
                    ijk = new int3(1, 0, 0);
                else if (f.X >= f.Z)
                    ijk = new int3(1, 0, 1);
                else
                    ijk = new int3(0, 0, 1);
            }
            else
            {
                if (f.Y < f.Z)
                    ijk = new int3(0, 1, 0);
                else if (f.X < f.Z)
                    ijk = new int3(0, 1, 1);
                else
                    ijk = new int3(1, 1, 0);
            }

            // Step 4: Get all 4 vertices of the tetrahedron
            float unskew = (i.X + i.Y + i.Z) * G3;
            float3 p0 = (float3)i - unskew;

            unskew = (i.X + ijk.X + i.Y + ijk.Y + i.Z + ijk.Z) * G3;
            float3 p1 = (float3)(i + ijk) - unskew;

            int3 i2 = i + new int3(
                ijk.X == 0 ? 1 : 0,
                ijk.Y == 0 ? 1 : 0,
                ijk.Z == 0 ? 1 : 0
            );
            unskew = (i2.X + i2.Y + i2.Z) * G3;
            float3 p2 = (float3)i2 - unskew;

            int3 i3 = i + new int3(1, 1, 1);
            unskew = (i3.X + i3.Y + i3.Z) * G3;
            float3 p3 = (float3)i3 - unskew;

            // Step 5: Vectors from vertices to point
            float3 d0 = p - p0;
            float3 d1 = p - p1;
            float3 d2 = p - p2;
            float3 d3 = p - p3;

            // Step 6: Get gradients
            uint hash0 = Hash3(i);
            uint hash1 = Hash3(i + ijk);
            uint hash2 = Hash3(i2);
            uint hash3 = Hash3(i3);

            float3 grad0 = Grad3(hash0 % 16);
            float3 grad1 = Grad3(hash1 % 16);
            float3 grad2 = Grad3(hash2 % 16);
            float3 grad3 = Grad3(hash3 % 16);

            // Step 7: Calculate contributions with falloff
            float t0 = Falloff(0.6f - Hlsl.Dot(d0, d0));
            float t1 = Falloff(0.6f - Hlsl.Dot(d1, d1));
            float t2 = Falloff(0.6f - Hlsl.Dot(d2, d2));
            float t3 = Falloff(0.6f - Hlsl.Dot(d3, d3));

            float n0 = t0 * Hlsl.Dot(grad0, d0);
            float n1 = t1 * Hlsl.Dot(grad1, d1);
            float n2 = t2 * Hlsl.Dot(grad2, d2);
            float n3 = t3 * Hlsl.Dot(grad3, d3);

            // Step 8: Sum and map to [0,1]
            // The multiplier 32.0 normalizes the range to [-1,1]
            return (n0 + n1 + n2 + n3) * 16.0f + 0.5f;
        }

        // ============================================================
        // HELPER FUNCTIONS
        // ============================================================

        /// <summary>
        /// Fractal Brownian Motion - sums multiple octaves of noise
        /// </summary>
        public static float FBM2D(float2 p, int octaves, float persistence, float lacunarity)
        {
            float value = 0;
            float amplitude = 0.5f;
            float frequency = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                value += amplitude * Noise2D(p * frequency);
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value;
        }

        /// <summary>
        /// Fractal Brownian Motion for 3D noise
        /// </summary>
        public static float FBM3D(float3 p, int octaves, float persistence, float lacunarity)
        {
            float value = 0;
            float amplitude = 0.5f;
            float frequency = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                value += amplitude * Noise3D(p * frequency);
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return value;
        }

        /// <summary>
        /// Domain-warped noise for more natural features
        /// </summary>
        public static float WarpedNoise3D(float3 p, int warpOctaves, float warpStrength)
        {
            // Generate warp vector using noise
            float3 warp = new float3(
                Noise3D(p + new float3(1000, 0, 0)) * 2 - 1,
                Noise3D(p + new float3(0, 1000, 0)) * 2 - 1,
                Noise3D(p + new float3(0, 0, 1000)) * 2 - 1
            ) * warpStrength;

            // Sample noise at warped position
            return Noise3D(p + warp);
        }

        /// <summary>
        /// Ridge noise (creates sharp ridges like mountains)
        /// </summary>
        public static float RidgeNoise3D(float3 p, int octaves)
        {
            float value = 0;
            float amplitude = 0.5f;
            float frequency = 1.0f;
            float weight = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                float noise = Noise3D(p * frequency);
                noise = 1.0f - Hlsl.Abs(noise); // Create ridges
                noise *= noise * weight;
                weight = noise;

                value += noise * amplitude;
                amplitude *= 0.5f;
                frequency *= 2.0f;
            }

            return value;
        }
    }
}
