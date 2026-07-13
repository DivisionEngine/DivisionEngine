//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;
using DivisionEngine.Rendering.ShaderUtilities;

namespace DivisionEngine.Rendering.Terrains
{
    /// <summary>
    /// Stores data for a single terrain cell.
    /// </summary>
    public struct TerrainData
    {
        public float height;
        public float2 slope;
    }

    /// <summary>
    /// Metadata for a precomputed terrain heightfield.
    /// </summary>
    public struct TerrainMetadata
    {
        public float3 worldMin;
        public float3 worldMax;
        public int2 resolution;  // Width, Height
        public int bufferOffset;  // Starting index in the big buffer
        public float heightScale;
        public float heightOffset;
        public int terrainIndex;  // Which terrain this is
        public float size;        // Original terrain scale
    }

    public static class TerrainRendering
    {
        // Constants
        private const float TAU = 6.28318530717959f;

        // Hash function required for Phacelle noise
        private static float2 Hash(float2 x)
        {
            float2 k = new float2(0.3183099f, 0.3678794f);
            x = x * k + new float2(k.Y, k.X);
            return -1.0f + 2.0f * Hlsl.Frac(16.0f * k * Hlsl.Frac(x.X * x.Y * (x.X + x.Y)));
        }

        public static float PowInv(float t, float power)
        {
            // Flip, raise to the specified power, and flip back
            return 1.0f - Hlsl.Pow(1.0f - Hlsl.Saturate(t), power);
        }

        public static float EaseOut(float t)
        {
            float v = 1.0f - Hlsl.Saturate(t);
            return 1.0f - v * v;
        }

        public static float SmoothStart(float t, float smoothing)
        {
            if (t >= smoothing)
                return t - 0.5f * smoothing;
            return 0.5f * t * t / smoothing;
        }

        public static float2 SafeNormalize(float2 n)
        {
            float l = Hlsl.Length(n);
            return (Hlsl.Abs(l) > 1e-10f) ? (n / l) : n;
        }

        // Phacelle Noise function - produces stripe patterns aligned with input vector
        public static float4 PhacelleNoise(float2 p, float2 normDir, float freq, float offset, float normalization)
        {
            float2 sideDir = new float2(-normDir.Y, normDir.X) * freq * TAU;
            offset *= TAU;

            float2 pInt = Hlsl.Floor(p);
            float2 pFrac = Hlsl.Frac(p);
            float2 phaseDir = float2.Zero;
            float weightSum = 0.0f;

            for (int i = -1; i <= 2; i++)
            {
                for (int j = -1; j <= 2; j++)
                {
                    float2 gridOffset = new float2(i, j);
                    float2 gridPoint = pInt + gridOffset; // FIX: hash the grid POINT not the offset
                    float2 randomOffset = Hash(gridPoint) * 0.5f;

                    float2 vectorFromCellPoint = pFrac - gridOffset - randomOffset;

                    float sqrDist = Hlsl.Dot(vectorFromCellPoint, vectorFromCellPoint);
                    float weight = Hlsl.Exp(-sqrDist * 2.0f);
                    weight = Hlsl.Max(0.0f, weight - 0.01111f);
                    weightSum += weight;

                    float waveInput = Hlsl.Dot(vectorFromCellPoint, sideDir) + offset;
                    phaseDir += new float2(Hlsl.Cos(waveInput), Hlsl.Sin(waveInput)) * weight;
                }
            }

            float2 interpolated = phaseDir / weightSum;
            float magnitude = Hlsl.Sqrt(Hlsl.Dot(interpolated, interpolated));
            magnitude = Hlsl.Max(1.0f - normalization, magnitude);
            return new float4(interpolated / magnitude, sideDir);
        }

        public static float3 FractalNoiseWithDerivatives(float2 p, float freq, int octaves, float lacunarity, float gain)
        {
            float3 n = float3.Zero;
            float nf = freq;
            float na = 1.0f;

            for (int i = 0; i < octaves; i++)
            {
                float3 noiseVal = GradientNoise.noised(p * nf);
                n += noiseVal * na * new float3(1.0f, nf, nf);
                na *= gain;
                nf *= lacunarity;
            }
            return n;
        }

        // Main erosion filter
        public static float4 ErosionFilter(
            float2 p,
            float3 heightAndSlope,
            float fadeTarget,
            float strength,
            float gullyWeight,
            float detail,
            float4 rounding,
            float4 onset,
            float2 assumedSlope,
            float scale,
            int octaves,
            float lacunarity,
            float gain,
            float cellScale,
            float normalization,
            out float ridgeMap,
            out float debug)
        {
            strength *= scale;
            fadeTarget = Hlsl.Clamp(fadeTarget, -1.0f, 1.0f);

            float3 inputHeightAndSlope = heightAndSlope;
            float freq = 1.0f / (scale * cellScale);
            float slopeLength = Hlsl.Max(Hlsl.Length(new float2(heightAndSlope.Y, heightAndSlope.Z)), 1e-10f);
            float magnitude = 0.0f;
            float roundingMult = 1.0f;

            float roundingForInput = Hlsl.Lerp(rounding.Y, rounding.X, Hlsl.Saturate(fadeTarget + 0.5f)) * rounding.Z;
            float combiMask = EaseOut(SmoothStart(slopeLength * onset.X, roundingForInput * onset.X));

            // Initialize ridgeMap
            float ridgeMapCombiMask = EaseOut(slopeLength * onset.Z);
            float ridgeMapFadeTarget = fadeTarget;

            // Determine gully direction based on slope mix
            float2 gullySlope = Hlsl.Lerp(new float2(heightAndSlope.Y, heightAndSlope.Z),
                                           new float2(heightAndSlope.Y, heightAndSlope.Z) / slopeLength * assumedSlope.X,
                                           assumedSlope.Y);

            for (int i = 0; i < octaves; i++)
            {
                float4 phacelle = PhacelleNoise(p * freq, SafeNormalize(gullySlope), cellScale, 0.25f, normalization);
                phacelle.ZW *= -freq; // negate and scale derivative direction

                float sloping = Hlsl.Abs(phacelle.Y);

                // FIX: gullySlope update — phacelle.ZW is already a float2 direction
                gullySlope += Hlsl.Sign(phacelle.Y) * new float2(phacelle.Z, phacelle.W) * strength * gullyWeight;

                // FIX: gullies — phacelle.Y * phacelle.ZW is a float2, not two separate floats
                float2 gulliesDerivative = phacelle.Y * new float2(phacelle.Z, phacelle.W);
                float3 gullies = new float3(phacelle.X, gulliesDerivative.X, gulliesDerivative.Y);

                float3 fadedGullies = Hlsl.Lerp(new float3(fadeTarget, 0.0f, 0.0f), gullies * gullyWeight, combiMask);

                heightAndSlope += fadedGullies * strength;
                magnitude += strength;

                // Update fadeTarget
                fadeTarget = fadedGullies.X;

                // Update mask
                float roundingForOctave = Hlsl.Lerp(rounding.Y, rounding.X, Hlsl.Saturate(phacelle.X + 0.5f)) * roundingMult;
                float newMask = EaseOut(SmoothStart(sloping * onset.Y, roundingForOctave * onset.Y));
                combiMask = PowInv(combiMask, detail) * newMask;

                // Update ridgeMap
                ridgeMapFadeTarget = Hlsl.Lerp(ridgeMapFadeTarget, gullies.X, ridgeMapCombiMask);
                float newRidgeMapMask = EaseOut(sloping * onset.W);
                ridgeMapCombiMask *= newRidgeMapMask;

                // Prepare next octave
                strength *= gain;
                freq *= lacunarity;
                roundingMult *= rounding.W;
            }

            ridgeMap = ridgeMapFadeTarget * (1.0f - ridgeMapCombiMask);
            debug = fadeTarget;

            float3 heightAndSlopeDelta = heightAndSlope - inputHeightAndSlope;
            return new float4(heightAndSlopeDelta, magnitude);
        }
    }
}
