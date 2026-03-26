//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;

namespace DivisionEngine
{

    /// <summary>
    /// Formats debug information from the depthNormals mask into an easier form to visualize.
    /// </summary>
    /// <param name="renderTex">Rendered output</param>
    /// <param name="depthNormals">Depth and normal information</param>
    /// <param name="debugMode">Debug mode to employ</param>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFDebug3D(
        ReadWriteTexture2D<float4> renderTex,
        ReadWriteTexture2D<float4> depthNormals,
        ReadWriteBuffer<uint2> objectIdBuffer,
        int debugMode,
        int width) : IComputeShader
    {

        // Constants
        const float EPSILON = 0.0001f;
        const float PI = 3.141592654f;
        const float RECIPROCAL_PI = 1f / PI;

        /// <summary>
        /// Picks a random color from an integer.
        /// </summary>
        /// <param name="id">Object ID</param>
        /// <returns>Random hashed color</returns>
        private float3 IntToColor(uint id)
        {
            // Mix the bits using prime numbers
            uint hash = id;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;

            // Convert to float in [0,1] range
            float r = (hash & 0xFF) / 255.0f;
            float g = ((hash >> 8) & 0xFF) / 255.0f;
            float b = ((hash >> 16) & 0xFF) / 255.0f;

            // Ensure minimum brightness and saturation
            return Hlsl.Max(new float3(r, g, b), 0.2f);
        }

        private static float3 FresnelSchlick(float cosTheta, float3 f0)
        {
            return f0 + (float3.One - f0) * Hlsl.Pow(1f - cosTheta, 5f);
        }

        /// <summary>
        /// Calculates the diffuse factor for GGX.
        /// </summary>
        /// <param name="NoH">Normal dot Halfway</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>Diffuse GGX factor</returns>
        private static float D_GGX(float NoH, float alpha)
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
        private static float GSmith(float NoV, float NoL, float alpha)
        {
            return G1_GGX_Schlick(NoL, alpha) * G1_GGX_Schlick(NoV, alpha);
        }

        /// <summary>
        /// Calculates the GGX Schlick function.
        /// </summary>
        /// <param name="NoV">Normal dot View</param>
        /// <param name="alpha">Roughness squared</param>
        /// <returns>G1 factor for GGX Schlick</returns>
        private static float G1_GGX_Schlick(float NoV, float alpha)
        {
            float k = alpha / 2f;
            return Hlsl.Max(NoV, EPSILON) / (NoV * (1f - k) + k);
        }

        private static float3 DebugBRDF(float3 N, float3 V, float3 L, float roughness, float reflectance)
        {
            float3 H = Hlsl.Normalize(V + L);
            float NdotV = Hlsl.Max(Hlsl.Dot(N, V), 0.0f);
            float NdotL = Hlsl.Max(Hlsl.Dot(N, L), 0.0f);
            float NdotH = Hlsl.Max(Hlsl.Dot(N, H), 0.0f);

            float D = D_GGX(NdotH, roughness);
            float G = G1_GGX_Schlick(NdotV, roughness) * G1_GGX_Schlick(NdotL, roughness);

            float3 f0 = float3.One * 0.16f * reflectance * reflectance;
            float3 F = FresnelSchlick(Hlsl.Max(Hlsl.Dot(V, H), 0.0f), f0);

            // Return RGB with R = D, G = G, B = average(F)
            return new float3(D, G, (F.X + F.Y + F.Z) / 3.0f);
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY; // Get pixel position
            if (debugMode == 1) // Depth buffer
            {
                float depth = depthNormals[pixel].R;
                renderTex[pixel] = new float4(depth, depth, depth, 1);
            }
            else if (debugMode == 2) // World normal buffer
                renderTex[pixel] = new float4(depthNormals[pixel].GBA, 1); 
            else if (debugMode == 3)  // Object ID buffer
            {
                float3 objColor = IntToColor(objectIdBuffer[pixel.X + pixel.Y * width].X);
                renderTex[pixel] = new float4(objColor, 1);
            }
            else if (debugMode == 4)
            {
                float3 objColor = IntToColor(objectIdBuffer[pixel.X + pixel.Y * width].X);
                renderTex[pixel] = new float4(objColor, 1);
            }
            else if (debugMode == 5)
            {
                float3 objColor = IntToColor(objectIdBuffer[pixel.X + pixel.Y * width].X);
                renderTex[pixel] = new float4(objColor, 1);
            }
            else if (debugMode == 6)
            {
                //float3 objColor = DebugBRDF(objectIdBuffer[pixel.X + pixel.Y * width].X);
                //renderTex[pixel] = new float4(objColor, 1);
            }
            else renderTex[pixel] = new float4(0, 0, 0, 1); // Default path --> clear output
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
