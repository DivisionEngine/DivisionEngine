//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;

namespace DivisionEngine.Rendering.ShaderUtilities
{
    /// <summary>
    /// Functions for physically based rendering workflows in shaders.
    /// </summary>
    public static class PBR
    {
        public static float3 FresnelSchlick(float cosTheta, float3 f0)
        {
            return f0 + (float3.One - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        /// <summary>
        /// Calculates the diffuse factor for GGX.
        /// </summary>
        /// <param name="NoH">Normal dot Halfway</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>Diffuse GGX factor</returns>
        public static float D_GGX(float NoH, float alpha, float RECIPROCAL_PI)
        {
            float alpha2 = alpha * alpha;
            float NoH2 = NoH * NoH;
            float b = NoH2 * (alpha2 - 1f) + 1f;
            return alpha2 * RECIPROCAL_PI / (b * b);
        }

        /// <summary>
        /// Combines GGX Schlick functions for difference vectors.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="NoL">Normal dot Light</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>G Smith value</returns>
        public static float GSmith(float NoV, float NoL, float alpha, float EPSILON)
        {
            return G1_GGX_Schlick(NoL, alpha, EPSILON) * G1_GGX_Schlick(NoV, alpha, EPSILON);
        }

        /// <summary>
        /// Calculates the GGX Schlick function.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>G1 factor for GGX Schlick</returns>
        public static float G1_GGX_Schlick(float NoV, float alpha, float EPSILON)
        {
            float k = alpha / 2f;
            return Hlsl.Max(NoV, EPSILON) / (NoV * (1f - k) + k);
        }

        /// <summary>
        /// Fresnel equation used by Disney.
        /// </summary>
        /// <param name="cosTheta">Cos theta angle</param>
        /// <param name="f0">Fresnel term</param>
        /// <param name="f90">90 degree fresnel term</param>
        /// <returns>Fresnel value</returns>
        public static float FresnelSchlick90(float cosTheta, float f0, float f90)
        {
            return f0 + (f90 - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        /// <summary>
        /// Calculates the diffuse factor for the BRDF using Disney's method.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="NoL">Normal dot Light</param>
        /// <param name="VoH">View dot Halfway</param>
        /// <param name="alpha">Roughness value squared</param>
        /// <returns>Disney diffuse factor for BRDF</returns>
        public static float DisneyDiffuseFactor(float NoV, float NoL, float VoH, float alpha)
        {
            float f90 = 0.5f + 2f * alpha * VoH * VoH;
            float F_In = FresnelSchlick90(NoL, 1f, f90);
            float F_Out = FresnelSchlick90(NoV, 1f, f90);
            return F_In * F_Out;
        }

        /// <summary>
        /// The bi-directional reflectance distributionfunction using Cook-Torrance.
        /// </summary>
        /// <param name="lightDir">Light direction</param>
        /// <param name="viewDir">View direction</param>
        /// <param name="finalNormal">Normal vector</param>
        /// <param name="sdf">SDF object to sample</param>
        /// <returns>BRDF output value</returns>
        public static float3 BRDFMicrofacetFunction(float3 lightDir, float3 viewDir, float3 finalNormal,
            float3 baseCol, float metallic, float roughAlpha, float specular, float RECIPROCAL_PI, float EPSILON)
        {
            float NoV = Hlsl.Saturate(Hlsl.Dot(finalNormal, viewDir));
            float NoL = Hlsl.Saturate(Hlsl.Dot(finalNormal, lightDir));
            float3 halfwayDir = Hlsl.Normalize(viewDir + lightDir);
            float VoH = Hlsl.Saturate(Hlsl.Dot(viewDir, halfwayDir));
            float NoH = Hlsl.Saturate(Hlsl.Dot(finalNormal, halfwayDir));

            float3 f0 = float3.One * 0.16f * specular * specular;
            f0 = Hlsl.Lerp(f0, baseCol, new float3(metallic, metallic, metallic));

            float3 F = FresnelSchlick(VoH, f0);
            float D = D_GGX(NoH, roughAlpha, RECIPROCAL_PI);
            float G = GSmith(NoV, NoL, roughAlpha, EPSILON);

            // Add epsilon to denominator to prevent division by near-zero
            float denominator = 4f * Hlsl.Max(NoV * NoL, EPSILON);
            float3 specularTerm = Hlsl.Min(F * D * G / denominator, 20f); // Clamp specular to prevent fireflies

            float3 rhoD = baseCol * DisneyDiffuseFactor(NoV, NoL, VoH, roughAlpha); // alternative: rhoD *= 1f - metallic;
            float3 diff = rhoD * RECIPROCAL_PI;

            // Hard cap to prevent explosions
            return Hlsl.Min(diff + specularTerm, 100f);
        }

        /// <summary>
        /// Calculates fresnel reflectance for dielectrics (glass, water, etc.)
        /// </summary>
        public static float SimpleFresnelDielectric(float cosθ, float f0)
        {
            // Schlick approximation (close enough for most cases)
            return f0 + (1f - f0) * Hlsl.Pow(1f - cosθ, 5f);
        }

        // Reflections functions:

        public static uint HaltonHash(uint x)
        {
            x = x ^ 61 ^ (x >> 16);
            x += x << 3;
            x ^= x >> 4;
            x *= 0x27d4eb2d;
            x ^= x >> 15;
            return x;
        }

        // Halton sequence generator
        public static float HaltonSequence(int index, int baseNum)
        {
            float result = 0f;
            float f = 1f;
            int i = index;
            while (i > 0)
            {
                f /= baseNum;
                result += f * (i % baseNum);
                i /= baseNum;
            }
            return result;
        }

        // Generate 2D Halton sample
        public static float2 Halton2D(int index)
        {
            return new float2(HaltonSequence(index, 2), HaltonSequence(index, 3));
        }

        // Importance sample GGX distribution for specular reflections, alpha = roughness * roughness
        public static float3 ImportanceSampleGGX(float2 u, float3 normal, float alpha, float PI)
        {
            float phi = 2f * PI * u.X;
            float cosTheta = Hlsl.Sqrt((1f - u.Y) / (1f + (alpha * alpha - 1f) * u.Y));
            float sinTheta = Hlsl.Sqrt(1f - cosTheta * cosTheta);

            // Spherical to cartesian
            float3 h = new float3(Hlsl.Cos(phi) * sinTheta, Hlsl.Sin(phi) * sinTheta, cosTheta);

            // Tangent space to world space
            float3 up = Hlsl.Abs(normal.Z) < 0.999f ? new float3(0, 0, 1) : new float3(1, 0, 0);
            float3 tangent = Hlsl.Normalize(Hlsl.Cross(up, normal));
            float3 bitangent = Hlsl.Cross(normal, tangent);
            return Hlsl.Normalize(tangent * h.X + bitangent * h.Y + normal * h.Z);
        }
    }
}
