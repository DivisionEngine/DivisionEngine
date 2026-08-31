//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;
using DivisionEngine.Rendering.Terrain;

namespace DivisionEngine.Rendering.ShaderUtilities
{
    /// <summary>
    /// Functions for getting distances to different SDF primitive objects for shaders exclusively.
    /// </summary>
    public static class SDFPrimitives
    {
        public static float Sphere(float3 pt, float r)
        {
            //float3 s = new float3(8, 8, 8);
            //float3 l = new float3(100, 1, 100);
            //float3 q = pt - s * Hlsl.Clamp(Hlsl.Round(pt / s), -l, l);
            return Hlsl.Length(pt) - r;
        }

        public static float Box(float3 pt, float3 size)
        {
            float3 q = Hlsl.Abs(pt) - size;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f);

            //float3 s = new float3(8, 8, 8);
            //float3 l = new float3(10000, 6, 10000);
            //float3 q = pt - s * Hlsl.Clamp(Hlsl.Round(pt / s), -l, l);

            //float sd1 = TorusSDF(q, size.XY);
            //float sd2 = PyramidSDF(q, size.X);

            //return Hlsl.Lerp(sd1, sd2, Hlsl.Saturate(worldData.fogAnisotropy));
        }

        public static float RoundedBox(float3 pt, float3 size, float r)
        {
            float3 q = Hlsl.Abs(pt) - size + r;
            return Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, Hlsl.Max(q.Y, q.Z)), 0.0f) - r;
        }

        public static float Torus(float3 pt, float2 tr)
        {
            float2 q = new float2(Hlsl.Length(pt.XZ) - tr.X, pt.Y);
            return Hlsl.Length(q) - tr.Y;
        }

        public static float Pyramid(float3 pt, float h)
        {
            float m2 = h * h + 0.25f;

            pt.XZ = Hlsl.Abs(pt.XZ);
            pt.XZ = (pt.Z > pt.X) ? pt.ZX : pt.XZ;
            pt.XZ -= 0.5f;

            float3 q = new float3(pt.Z, h * pt.Y - 0.5f * pt.X, h * pt.X + 0.5f * pt.Y);

            float s = Hlsl.Max(-q.X, 0.0f);
            float t = Hlsl.Saturate((q.Y - 0.5f * pt.Z) / (m2 + 0.25f));

            float a = m2 * (q.X + s) * (q.X + s) + q.Y * q.Y;
            float b = m2 * (q.X + 0.5f * t) * (q.X + 0.5f * t) + (q.Y - m2 * t) * (q.Y - m2 * t);

            float d2 = Hlsl.Min(q.Y, -q.X * m2 - q.Y * 0.5f) > 0.0f ? 0.0f : Hlsl.Min(a, b);

            return Hlsl.Sqrt((d2 + q.Z * q.Z) / m2) * Hlsl.Sign(Hlsl.Max(q.Z, -pt.Y));
        }

        public static float Plane(float3 pt, float3 n, float h)
        {
            return Hlsl.Dot(pt, Hlsl.Normalize(n)) + h;
        }

        // Bound not exact, for performance
        public static float Cone(float3 pt, float2 c, float h)
        {
            float q = Hlsl.Length(pt.XZ);
            return Hlsl.Max(Hlsl.Dot(c.XY, new float2(q, pt.Y)), -h - pt.Y);
        }

        // Vertical version, for performance
        public static float Cylinder(float3 pt, float r, float h)
        {
            float2 d = Hlsl.Abs(new float2(Hlsl.Length(pt.XZ), pt.Y)) - new float2(r, h);
            return Hlsl.Min(Hlsl.Max(d.X, d.Y), 0.0f) + Hlsl.Length(Hlsl.Max(d, 0.0f));
        }

        // Vertical version, for performance
        public static float Capsule(float3 pt, float r, float h)
        {
            pt.Y -= Hlsl.Clamp(pt.Y, 0.0f, h);
            return Hlsl.Length(pt) - r;
        }

        #region terrain

        /// <summary>
        /// Tapered capsule SDF with bend - ideal for grass blades
        /// </summary>
        public static float TaperedCapsuleBend(float3 pt, float rBottom, float rTop, float height, float bendAmount, float2 bendDirection)
        {
            // Bend the point
            float bendFactor = Hlsl.Saturate(pt.Y / height);
            float bendScale = bendFactor * bendFactor;
            float3 bentPt = pt;
            bentPt.X += bendScale * bendAmount * bendDirection.X;
            bentPt.Z += bendScale * bendAmount * bendDirection.Y;

            // Tapered capsule
            float t = Hlsl.Clamp(bentPt.Y / height, 0.0f, 1.0f);
            float radius = Hlsl.Lerp(rBottom, rTop, t);

            // Get distance to capsule centerline
            float2 horizontal = new float2(bentPt.X, bentPt.Z);
            float dist = Hlsl.Length(horizontal) - radius;

            // Add vertical clipping
            float vertical = Hlsl.Abs(bentPt.Y - height * 0.5f) - height * 0.5f;

            return Hlsl.Max(dist, vertical);
        }

        public static float Terrain(float3 pt, float size, float heightScale, float persistence, float lacunarity, int octaves)
        {
            float height = GradientNoise.FBMNoised(pt.XZ / size, octaves, persistence, lacunarity, out float2 deriv);
            float terrainHeight = height * heightScale - (heightScale / 2f);

            //float slope = Hlsl.Length(deriv) * heightScale / size;

            float terrain = pt.Y - terrainHeight;
            return terrain;
        }

        public static float TerrainEroded(float3 pt, float size, float heightScale, float baseGain, float lacunarity,
            float erosionStrength, float gullyWeight, float erosionDetail, float erosionScale, int erosionOctaves,
            float erosionLacunarity, float erosionGain, float cellScale, float normalization, int baseOctaves, float4 rounding)
        {
            // Base terrain using FBM
            float2 uv = pt.XZ / size;

            // Calculate base height using the noised function
            float3 baseTerrain = TerrainRendering.FractalNoiseWithDerivatives(uv, 3.0f, baseOctaves, lacunarity, baseGain);
            float baseHeightNorm = baseTerrain.X * 0.5f + 0.5f;
            float2 slope = new float2(baseTerrain.Y, baseTerrain.Z);

            // Erosion filter
            float fadeTarget = Hlsl.Clamp(baseTerrain.X / 0.6f, -1.0f, 1.0f);
            float4 onset = new float4(1.25f, 1.25f, 2.8f, 1.5f);
            float2 assumedSlope = new float2(0.7f, 1.0f);

            float ridgeMap, debug;
            float3 heightSlope = new float3(baseHeightNorm, slope.X, slope.Y);
            float4 erosionResult = TerrainRendering.ErosionFilter(
                uv, heightSlope, fadeTarget,
                erosionStrength, gullyWeight, erosionDetail,
                rounding, onset, assumedSlope,
                erosionScale, erosionOctaves, erosionLacunarity,
                erosionGain, cellScale, normalization,
                out ridgeMap, out debug);

            // Apply height offset
            float heightOffset = -0.65f;
            float offset = heightOffset * erosionResult.W;
            float erodedHeightNorm = baseHeightNorm + erosionResult.X + offset;
            float erodedHeight = (erodedHeightNorm - 0.5f) * heightScale;

            return pt.Y - erodedHeight;
        }

        /// <summary>
        /// Combines terrain with grass blades using repeating SDFs
        /// </summary>
        public static float TerrainWithGrassBaked(float terrainHeight, float3 pt,
            float grassDensity, float grassHeight, float grassRadius, float bendAmount,
            int terrainIndex, float time, float2 windDirection, float windStrength)
        {
            float terrainDist = pt.Y - terrainHeight;

            float margin = grassHeight * 0.6f + grassRadius * 2f;
            if (terrainDist > margin) return terrainDist;

            float2 worldXZ = pt.XZ;
            float cellSize = 1.0f / grassDensity;
            float2 baseCellIndex = Hlsl.Floor(worldXZ / cellSize);

            float grassDist = 1e8f;

            // 3x3 grid sampling for overlapping blades
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    float2 cellIndex = baseCellIndex + new float2(dx, dz);

                    // Jitter using cell index as seed
                    float2 jitter = new float2(
                        SimplexNoise.Noise2D(cellIndex * 13.7f + new float2(0.5f, 0.5f)) * 2f - 1f,
                        SimplexNoise.Noise2D(cellIndex * 29.3f + new float2(0.5f, 0.5f)) * 2f - 1f
                    ) * cellSize * 0.4f;

                    float2 cellCenterWorld = (cellIndex + 0.5f) * cellSize + jitter;

                    // Random blade parameters
                    float randomHeight = SimplexNoise.Noise2D(cellIndex * 7.1f + new float2(0.3f, 0.7f)) * 0.5f + 0.5f;
                    float randomBend = SimplexNoise.Noise2D(cellIndex * 11.3f + new float2(0.7f, 0.3f)) * 0.5f + 0.5f;
                    float randomRadius = SimplexNoise.Noise2D(cellIndex * 19.7f + new float2(0.1f, 0.9f)) * 0.5f + 0.5f;
                    float randomRotation = SimplexNoise.Noise2D(cellIndex * 23.1f + new float2(0.9f, 0.1f)) * 6.28318530718f;

                    // Blade height at root position (sample terrain at blade root)
                    float bladeGroundHeight = terrainHeight; // Simplified

                    float2 offsetXZ = worldXZ - cellCenterWorld;
                    float s = Hlsl.Sin(randomRotation);
                    float c = Hlsl.Cos(randomRotation);
                    float2 rotatedXZ = new float2(
                        offsetXZ.X * c - offsetXZ.Y * s,
                        offsetXZ.X * s + offsetXZ.Y * c
                    );

                    float3 grassLocal = new float3(rotatedXZ.X, pt.Y - bladeGroundHeight, rotatedXZ.Y);

                    // Wind animation
                    float windPhase = SimplexNoise.Noise2D(cellIndex * 3.3f + new float2(0.7f, 0.3f)) * 6.28318530718f;
                    float windBend = Hlsl.Sin(time * 1.6f + windPhase) * windStrength * 0.3f;

                    // Wind direction influence
                    float windDot = Hlsl.Dot(windDirection, new float2(rotatedXZ.X, rotatedXZ.Y));
                    float windInfluence = Hlsl.Saturate(windDot * 0.5f + 0.5f);

                    float rBottom = grassRadius * (0.8f + randomRadius * 0.4f);
                    float rTop = grassRadius * 0.05f * randomRadius;
                    float h = grassHeight * (0.5f + randomHeight * 0.5f);
                    float bend = (bendAmount * randomBend * 0.6f + windBend) * windInfluence;

                    // Use the fixed tapered capsule
                    float bladeDist = TaperedCapsuleBend(grassLocal, rBottom, rTop, h, bend, windDirection);
                    grassDist = Hlsl.Min(grassDist, bladeDist);
                }
            }

            return Hlsl.Min(terrainDist, grassDist);
        }


        #endregion terrain
    }
}
