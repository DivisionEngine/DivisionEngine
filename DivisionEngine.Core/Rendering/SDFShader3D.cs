//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
#pragma warning disable CA1416 // Validate platform compatibility

using ComputeSharp;
using DivisionEngine.Rendering;
using DivisionEngine.Rendering.ShaderUtilities;

namespace DivisionEngine
{
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct SDFShader3D(
        float width,
        float height,
        float aspect,
        int frameCount,
        int debugMode,
        int enableCheckerboard,
        ReadOnlyBuffer<SDFWorldDTO> worldData,
        ReadWriteTexture2D<float4> texture,
        ReadWriteTexture2D<float4> depthNormals,
        ReadWriteBuffer<uint2> entityIdBuffer,
        ReadOnlyBuffer<SDFObjectDTO> sdfObjects,
        ReadOnlyBuffer<SDFLightDTO> lights,
        ReadOnlyBuffer<uint> textureData,
        ReadOnlyBuffer<TextureMetadata> textureMetadata) : IComputeShader
    {
        // Main constants
        const float EPSILON = 0.0001f;
        const float PI = 3.141592654f;
        const float RECIPROCAL_PI = 1f / PI;
        const float MIN_TRAVERSE_DIST = 100000000.0f;

        // Reflection constants
        const int SAMPLES_PER_PIXEL = 2;
        const float MIN_REFLECTION_CHANCE = 0.01f;
        const float MIN_THROUGHPUT = 0.01f;
        const float REFLECTION_BIAS = 2f; // Multiplier for normal offset

        #region textures

        public float SampleTexture(int textureId, float2 uv, float fallback)
        {
            return SampleTexture(textureId, uv, fallback * float4.One).R;
        }

        public float4 SampleTexture(int textureId, float2 uv, float4 fallbackColor)
        {
            if (textureId < 0 || textureId >= textureMetadata.Length) return fallbackColor;
            float2 newUV = new float2(Hlsl.Abs(uv.X % 1), Hlsl.Abs(uv.Y % 1));

            TextureMetadata meta = textureMetadata[textureId];
            int x = (int)(newUV.X * (meta.resolution.X - 1));
            int y = (int)(newUV.Y * (meta.resolution.Y - 1));
            int index = meta.bufferOffset + y * meta.resolution.X + x;
            return ShaderMath.UnpackRGBA(textureData[index]);
        }

        public float SampleTextureBilinear(int textureId, float2 uv, float fallback)
        {
            return SampleTextureBilinear(textureId, uv, fallback * float4.One).R;
        }

        public float4 SampleTextureBilinear(int textureId, float2 uv, float4 fallbackColor)
        {
            if (textureId < 0 || textureId >= textureMetadata.Length) return fallbackColor;
            float2 newUV = new float2(Hlsl.Abs(uv.X % 1), Hlsl.Abs(uv.Y % 1));

            TextureMetadata meta = textureMetadata[textureId];
            float u = newUV.X * (meta.resolution.X - 1);
            float v = newUV.Y * (meta.resolution.Y - 1);

            int x0 = (int)u;
            int y0 = (int)v;
            int x1 = Hlsl.Min(x0 + 1, meta.resolution.X - 1);
            int y1 = Hlsl.Min(y0 + 1, meta.resolution.Y - 1);

            float u_frac = u - x0;
            float v_frac = v - y0;

            int idx00 = meta.bufferOffset + y0 * meta.resolution.X + x0;
            int idx10 = meta.bufferOffset + y0 * meta.resolution.X + x1;
            int idx01 = meta.bufferOffset + y1 * meta.resolution.X + x0;
            int idx11 = meta.bufferOffset + y1 * meta.resolution.X + x1;

            float4 c00 = ShaderMath.UnpackRGBA(textureData[idx00]);
            float4 c10 = ShaderMath.UnpackRGBA(textureData[idx10]);
            float4 c01 = ShaderMath.UnpackRGBA(textureData[idx01]);
            float4 c11 = ShaderMath.UnpackRGBA(textureData[idx11]);

            float4 c0 = Hlsl.Lerp(c00, c10, u_frac);
            float4 c1 = Hlsl.Lerp(c01, c11, u_frac);
            return Hlsl.Lerp(c0, c1, v_frac);
        }

        /// <summary>
        /// Faster version using precomputed TBN matrix (reduces operations).
        /// </summary>
        public float3 SampleNormalMapFast(int textureId, float2 uv, float3 normal, float3 tangent, float strength)
        {
            float4 normalMap = SampleTextureBilinear(textureId, uv, new float4(0.5f, 0.5f, 1.0f, 1.0f));

            // Unpack normal
            float3 tangentNormal = new float3(
                (normalMap.R * 2.0f - 1.0f) * strength,
                (normalMap.G * 2.0f - 1.0f) * strength,
                normalMap.B * 2.0f - 1.0f
            );
            tangentNormal = Hlsl.Normalize(tangentNormal);

            // Transform to world space using TBN matrix
            return Hlsl.Normalize(
                tangent * tangentNormal.X +
                ShaderMath.CalcBitangent(normal, tangent) * tangentNormal.Y +
                normal * tangentNormal.Z
            );
        }

        private void GetMipInfo(TextureMetadata meta, int level, out int offset, out int2 res)
        {
            level = Hlsl.Clamp(level, 0, meta.mipCount - 1);
            offset = meta.bufferOffset;
            res = meta.resolution;
            for (int i = 0; i < level; i++)
            {
                offset += res.X * res.Y;
                res = new int2(Hlsl.Max(1, res.X / 2), Hlsl.Max(1, res.Y / 2));
            }
        }

        private float4 SampleTextureBilinearMip(int textureId, float2 uv, int mipLevel, float4 fallbackColor)
        {
            if (textureId < 0 || textureId >= textureMetadata.Length) return fallbackColor;
            TextureMetadata meta = textureMetadata[textureId];
            GetMipInfo(meta, mipLevel, out int offset, out int2 res);

            float2 newUV = new float2(Hlsl.Abs(uv.X % 1), Hlsl.Abs(uv.Y % 1));
            float u = newUV.X * (res.X - 1);
            float v = newUV.Y * (res.Y - 1);

            int x0 = (int)u; int y0 = (int)v;
            int x1 = Hlsl.Min(x0 + 1, res.X - 1);
            int y1 = Hlsl.Min(y0 + 1, res.Y - 1);
            float u_frac = u - x0; float v_frac = v - y0;

            float4 c00 = ShaderMath.UnpackRGBA(textureData[offset + y0 * res.X + x0]);
            float4 c10 = ShaderMath.UnpackRGBA(textureData[offset + y0 * res.X + x1]);
            float4 c01 = ShaderMath.UnpackRGBA(textureData[offset + y1 * res.X + x0]);
            float4 c11 = ShaderMath.UnpackRGBA(textureData[offset + y1 * res.X + x1]);

            float4 c0 = Hlsl.Lerp(c00, c10, u_frac);
            float4 c1 = Hlsl.Lerp(c01, c11, u_frac);
            return Hlsl.Lerp(c0, c1, v_frac);
        }

        private float4 SampleTextureTrilinear(int textureId, float2 uv, float mipLevel, float4 fallbackColor)
        {
            if (textureId < 0 || textureId >= textureMetadata.Length) return fallbackColor;
            int mipCount = textureMetadata[textureId].mipCount;
            float clampedLevel = Hlsl.Clamp(mipLevel, 0f, mipCount - 1);
            int level0 = (int)clampedLevel;
            int level1 = Hlsl.Min(level0 + 1, mipCount - 1);
            float frac = clampedLevel - level0;

            float4 c0 = SampleTextureBilinearMip(textureId, uv, level0, fallbackColor);
            float4 c1 = SampleTextureBilinearMip(textureId, uv, level1, fallbackColor);
            return Hlsl.Lerp(c0, c1, frac);
        }

        private float EstimateMipLevel(float depth, float scale, int2 textureResolution)
        {
            float texelWorldSize = (1f / Hlsl.Max(scale, EPSILON)) / Hlsl.Max(textureResolution.X, 1f);
            float pixelWorldSize = Hlsl.Max(depth * worldData[0].camScreenDist / height, depth * worldData[0].camScreenDist / width);
            return Hlsl.Max(0f, Hlsl.Log2(Hlsl.Max(pixelWorldSize / Hlsl.Max(texelWorldSize, EPSILON), 1f)));
        }

        #endregion textures
        #region triplanar

        /// <summary>
        /// Blend weights for triplanar projection based on a local-space surface normal.
        /// Higher sharpness = narrower blend seams, more distinct axis-aligned faces.
        /// </summary>
        private static float3 TriplanarWeights(float3 localNormal, float sharpness)
        {
            float3 blend = Hlsl.Pow(Hlsl.Abs(localNormal), sharpness);
            return blend / Hlsl.Max(blend.X + blend.Y + blend.Z, EPSILON);
        }

        /// <summary>
        /// Triplanar sample of a data texture (albedo, roughness, metallic).
        /// </summary>
        private float4 SampleTriplanar(int textureId, float3 localPos, float3 blend, float scale, float4 fallback)
        {
            float4 colX = SampleTextureBilinear(textureId, localPos.YZ * scale, fallback);
            float4 colY = SampleTextureBilinear(textureId, localPos.XZ * scale, fallback);
            float4 colZ = SampleTextureBilinear(textureId, localPos.XY * scale, fallback);
            return colX * blend.X + colY * blend.Y + colZ * blend.Z;
        }

        private float SampleTriplanarScalar(int textureId, float3 localPos, float3 blend, float scale, float fallback)
        {
            return SampleTriplanar(textureId, localPos, blend, scale, fallback * float4.One).R;
        }

        /// <summary>
        /// Triplanar normal map sample, swizzled directly into local object space.
        /// No arbitrary tangent basis is needed: each axis projection's tangent space
        /// IS just the two local axes it's projected onto, so the swizzle is exact
        /// rather than approximated.
        /// </summary>
        private float3 SampleNormalTriplanarLocal(int textureId, float3 localPos, float3 localNormal, float3 blend, float scale, float strength)
        {
            if (textureId < 0) return localNormal;

            float4 flat = new float4(0.5f, 0.5f, 1f, 1f);
            float4 mapX = SampleTextureBilinear(textureId, localPos.YZ * scale, flat);
            float4 mapY = SampleTextureBilinear(textureId, localPos.XZ * scale, flat);
            float4 mapZ = SampleTextureBilinear(textureId, localPos.XY * scale, flat);

            float3 tX = new float3((mapX.R * 2f - 1f) * strength, (mapX.G * 2f - 1f) * strength, mapX.B);
            float3 tY = new float3((mapY.R * 2f - 1f) * strength, (mapY.G * 2f - 1f) * strength, mapY.B);
            float3 tZ = new float3((mapZ.R * 2f - 1f) * strength, (mapZ.G * 2f - 1f) * strength, mapZ.B);

            // Keep the map's "outward" axis pointing the same way as the geometric normal
            float sx = Hlsl.Sign(localNormal.X);
            float sy = Hlsl.Sign(localNormal.Y);
            float sz = Hlsl.Sign(localNormal.Z);

            // X-projection: U=localY, V=localZ, tangent-Z = localX
            float3 nX = new float3(tX.Z * sx, tX.X, tX.Y);
            // Y-projection: U=localX, V=localZ, tangent-Z = localY
            float3 nY = new float3(tY.X, tY.Z * sy, tY.Y);
            // Z-projection: U=localX, V=localY, tangent-Z = localZ
            float3 nZ = new float3(tZ.X, tZ.Y, tZ.Z * sz);

            return Hlsl.Normalize(nX * blend.X + nY * blend.Y + nZ * blend.Z);
        }

        private float4 SampleTriplanarMip(int textureId, float3 localPos, float3 blend, float scale, float mipLevel, float4 fallback)
        {
            float4 colX = SampleTextureTrilinear(textureId, localPos.YZ * scale, mipLevel, fallback);
            float4 colY = SampleTextureTrilinear(textureId, localPos.XZ * scale, mipLevel, fallback);
            float4 colZ = SampleTextureTrilinear(textureId, localPos.XY * scale, mipLevel, fallback);
            return colX * blend.X + colY * blend.Y + colZ * blend.Z;
        }

        private float SampleTriplanarScalarMip(int textureId, float3 localPos, float3 blend, float scale, float mipLevel, float fallback)
        {
            return SampleTriplanarMip(textureId, localPos, blend, scale, mipLevel, fallback * float4.One).R;
        }

        /// <summary>
        /// Triplanar normal map sample with mipmapping.
        /// </summary>
        private float3 SampleNormalTriplanarMip(int textureId, float3 localPos, float3 localNormal, float3 blend,
            float scale, float strength, float mipLevel)
        {
            if (textureId < 0) return localNormal;

            float4 flat = new float4(0.5f, 0.5f, 1f, 1f);

            // Use trilinear/mipmapped sampling for each axis
            float4 mapX = SampleTextureTrilinear(textureId, localPos.YZ * scale, mipLevel, flat);
            float4 mapY = SampleTextureTrilinear(textureId, localPos.XZ * scale, mipLevel, flat);
            float4 mapZ = SampleTextureTrilinear(textureId, localPos.XY * scale, mipLevel, flat);

            float3 tX = new float3((mapX.R * 2f - 1f) * strength, (mapX.G * 2f - 1f) * strength, mapX.B);
            float3 tY = new float3((mapY.R * 2f - 1f) * strength, (mapY.G * 2f - 1f) * strength, mapY.B);
            float3 tZ = new float3((mapZ.R * 2f - 1f) * strength, (mapZ.G * 2f - 1f) * strength, mapZ.B);

            // Keep the map's "outward" axis pointing the same way as the geometric normal
            float sx = Hlsl.Sign(localNormal.X);
            float sy = Hlsl.Sign(localNormal.Y);
            float sz = Hlsl.Sign(localNormal.Z);

            // X-projection: U=localY, V=localZ, tangent-Z = localX
            float3 nX = new float3(tX.Z * sx, tX.X, tX.Y);
            // Y-projection: U=localX, V=localZ, tangent-Z = localY
            float3 nY = new float3(tY.X, tY.Z * sy, tY.Y);
            // Z-projection: U=localX, V=localY, tangent-Z = localZ
            float3 nZ = new float3(tZ.X, tZ.Y, tZ.Z * sz);

            return Hlsl.Normalize(nX * blend.X + nY * blend.Y + nZ * blend.Z);
        }

        #endregion triplanar
        #region sdf_sampling

        /// <summary>
        /// Single dispatch for a primitive's own local-space distance, excluding terrain.
        /// Takes only what it needs (type + one float4), not the full DTO, to keep
        /// per-call codegen light — this gets called multiple times per normal.
        /// </summary>
        private static float EvaluatePrimitiveDistanceFast(int type, float4 parameters, float3 localPt)
        {
            if (type == 0) return SDFPrimitives.Sphere(localPt, parameters.X);
            else if (type == 1) return SDFPrimitives.Box(localPt, parameters.XYZ);
            else if (type == 2) return SDFPrimitives.RoundedBox(localPt, parameters.XYZ, parameters.W);
            else if (type == 3) return SDFPrimitives.Torus(localPt, parameters.XY);
            else if (type == 4) return SDFPrimitives.Pyramid(localPt, parameters.X);
            else if (type == 5) return SDFPrimitives.Plane(localPt, parameters.XYZ, parameters.W);
            else if (type == 6) return SDFPrimitives.Cylinder(localPt, parameters.X, parameters.Y);
            else if (type == 7) return SDFPrimitives.Capsule(localPt, parameters.X, parameters.Y);
            else if (type == 8) return SDFPrimitives.Cone(localPt, parameters.XY, parameters.Z);
            else return SDFPrimitives.Sphere(localPt, parameters.X);
        }

        /// <summary>
        /// Calculates the SDF distance for the world at a point.
        /// </summary>
        /// <param name="point">World position to evaluate</param>
        /// <param name="shadowCastCheck">Should the tracer verify shadow casters</param>
        /// <returns>Float2 representing the min distance, and closest object</returns>
        private float WorldSDF(float3 point, bool shadowCastCheck, uint excludeID, out int closest)
        {
            float minDist = MIN_TRAVERSE_DIST;

            closest = -1;
            for (int i = 0; i < sdfObjects.Length; i++)
            {
                SDFObjectDTO curSDF = sdfObjects[i];
                if (shadowCastCheck && !curSDF.shadowEffects.X) continue;
                if (sdfObjects[i].entityId == excludeID) continue; // Exclude to get second-closest object
                float3 scaling = curSDF.scaling;
                float3 curPoint = point - curSDF.position; // Transform SDF
                curPoint = ShaderMath.RotateVector(curPoint, curSDF.rotation); // Rotate SDF
                curPoint *= scaling;

                // Scale distance function
                float dist = Hlsl.Min(scaling.X, Hlsl.Min(scaling.Y, scaling.Z));
                if (curSDF.type == 9) // Terrain: many-parameter dispatch, kept separate
                    dist *= SDFPrimitives.TerrainEroded(curPoint, curSDF.parameters.X, curSDF.parameters.Y, curSDF.parameters.Z, curSDF.parameters.W,
                        curSDF.parameters2.X, curSDF.parameters2.Y, curSDF.parameters2.Z, curSDF.parameters2.W,
                        (int)curSDF.parameters3.X, curSDF.parameters3.Y, curSDF.parameters3.Z, curSDF.parameters3.W,
                        curSDF.parameters4.X, (int)curSDF.parameters4.Y, curSDF.parameters5);
                else
                    dist *= EvaluatePrimitiveDistanceFast(curSDF.type, curSDF.parameters, curPoint);

                // Cheap displacement texture mapping
                if (curSDF.displaceTexMetaID >= 0)
                {
                    // Cheap pseudo-normal for triplanar blend weights (exact for sphere-like shapes,
                    // a reasonable approximation for box/rounded-box; less accurate for torus/plane/cylinder)
                    float3 pseudoNormal = Hlsl.Normalize(curPoint + EPSILON);
                    float3 dispBlend = TriplanarWeights(pseudoNormal, 4f);
                    float dispScale = 1f / Hlsl.Max(curSDF.texTilingOffset.X, EPSILON);

                    float height = SampleTriplanarScalar(curSDF.displaceTexMetaID, curPoint, dispBlend, dispScale, 0.5f) - 0.5f;
                    dist -= height * curSDF.displaceStrength;

                    // Displacement breaks the 1-Lipschitz guarantee the marcher relies on for safe step sizes,
                    // so shrink the step conservatively to avoid punching through fine detail
                    dist *= 0.5f;
                }

                dist *= curSDF.stepBias;
                if (Hlsl.Abs(dist) < minDist)
                {
                    closest = i;
                    minDist = dist;
                }
            }

            // Return packaged minimum SDF distance and closest object index
            return minDist;
        }

        #endregion sdf_sampling
        #region normals

        // --------------------
        // Normals and Tangents
        // --------------------

        /// <summary>
        /// Very fast high quality normal calculation (4 samples).
        /// </summary>
        /// <param name="pos">Hit position</param>
        /// <returns>World normal vector</returns>
        private float3 FastNormal(float3 pos) // for function f(p)
        {
            float h = EPSILON * 50; // replace by an appropriate value
            float2 k = new float2(1f, -1f);
            return Hlsl.Normalize(k.XYY * WorldSDF(pos + k.XYY * h, false, uint.MaxValue, out _) +
                              k.YYX * WorldSDF(pos + k.YYX * h, false, uint.MaxValue, out _) +
                              k.YXY * WorldSDF(pos + k.YXY * h, false, uint.MaxValue, out _) +
                              k.XXX * WorldSDF(pos + k.XXX * h, false, uint.MaxValue, out _));
        }

        /// <summary>
        /// Computes a surface normal using only the known hit object's own primitive
        /// field, instead of the full multi-object WorldSDF. Cuts normal calculation
        /// from O(4 * totalObjectCount) world samples down to O(4) — since WorldSDF is
        /// a hard-min union (no smooth blending), the gradient at a given hit point is
        /// exactly this object's own gradient there, so this isn't an approximation
        /// except at the measure-zero seam where two objects are exactly tied.
        /// Not used for terrain (type 9) — see ComputeSurfaceNormal below.
        /// </summary>
        private float3 FastNormalSingleObject(float3 pos, SDFObjectDTO sdf)
        {
            float h = EPSILON * 1000;
            float2 k = new float2(1f, -1f);
            return Hlsl.Normalize(k.XYY * EvaluatePrimitiveDistanceFast(sdf.type, sdf.parameters, pos + k.XYY * h) +
                              k.YYX * EvaluatePrimitiveDistanceFast(sdf.type, sdf.parameters, pos + k.YYX * h) +
                              k.YXY * EvaluatePrimitiveDistanceFast(sdf.type, sdf.parameters, pos + k.YXY * h) +
                              k.XXX * EvaluatePrimitiveDistanceFast(sdf.type, sdf.parameters, pos + k.XXX * h));
        }

        /// <summary>
        /// Picks the cheap single-object normal for ordinary primitives, and falls back
        /// to the full scene-aware WorldSDF path for terrain, which needs the erosion-
        /// aware field and is usually one large object anyway (so the multi-object cost
        /// matters less there).
        /// </summary>
        private float3 ComputeSurfaceNormal(float3 worldPos, SDFObjectDTO sdf)
        {
            if (sdf.type == 9) return FastNormal(worldPos);
            return FastNormalSingleObject(worldPos, sdf);
        }

        // Platform compliant version
        //private float3 FastNormal(float3 pos)
        //{
        //    float3 n = new float3(0f, 0f, 0f);
        //    for (int i = 0; i < 4; i++)
        //    {
        //        float3 e = 0.5773f * (2f * new float3(((i + 3) >> 1) & 1, (i >> 1) & 1, i & 1) - 1f);
        //        n += e * WorldSDF(pos + EPSILON * 50 * e, false, uint.MaxValue, out _);
        //        if (n.X + n.Y + n.Z > 100f) break;
        //    }
        //    return Hlsl.Normalize(n);
        //}

        #endregion normals
        #region lighting

        // --------------------
        // Lighting Calculation
        // --------------------

        // New soft-shadow technique:
        // Reference: https://iquilezles.org/articles/rmshadows/
        // New Version: https://www.shadertoy.com/view/tscSRS
        private float SoftShadow2(float3 point, float3 dir, float start, float end, out int closestObj)
        {
            float depth = start, dist;
            float shadow = 1f;
            closestObj = -1;

            for (int i = 0; i < worldData[0].maxShadowRaySteps; ++i)
            {
                dist = WorldSDF(point + depth * dir, true, uint.MaxValue, out closestObj);
                if (depth > end) break;
                //if (shadow < 0f) break; // Already fully in shadow, stop early

                shadow = Hlsl.Min(shadow, worldData[0].shadowScale * dist / depth);
                depth += Hlsl.Clamp(dist, 0.05f, 10f); // Larger minimum step than 0.01
            }

            shadow = Hlsl.Max(shadow, -1f);
            return Hlsl.SmoothStep(-1f, 0f, shadow);
        }

        /// <summary>
        /// Calculate lighting contribution from all lights
        /// </summary>
        private float3 CalculateLighting(float3 hitPoint, float3 geoNormal, float3 finalNormal, float3 viewDir,
            bool2 shadowEffects, float2 shadowDistances, float specular, float3 baseCol, float metallic, float roughAlpha)
        {
            float3 totalLight = float3.Zero;
            float lightShadow = 1f;

            for (int i = 0; i < lights.Length; i++)
            {
                SDFLightDTO light = lights[i];
                if (light.type == 0) // Directional
                {
                    float3 lightDir = Hlsl.Normalize(ShaderMath.RotateVector(new float3(0, 0, -1), light.rotation));
                    float3 lightColor = light.color.RGB * light.intensity;

                    float NoL = Hlsl.Max(Hlsl.Dot(finalNormal, lightDir), 0f);
                    if (NoL <= 0f) continue;

                    if (shadowEffects.Y)
                    {
                        float3 shadowOrigin = hitPoint + geoNormal * EPSILON * REFLECTION_BIAS;
                        lightShadow = Hlsl.Min(SoftShadow2(shadowOrigin, lightDir,
                            shadowDistances.X, shadowDistances.Y, out _), lightShadow);
                    }

                    float3 brdf = PBR.BRDFMicrofacetFunction(lightDir, viewDir,
                        finalNormal, baseCol, metallic, roughAlpha, specular, RECIPROCAL_PI, EPSILON);
                    totalLight += brdf * lightColor * NoL * lightShadow;
                }
                else if (light.type == 1) // Point
                {
                    float3 lightVec = light.position - hitPoint;
                    float distance = Hlsl.Length(lightVec);
                    float3 lightDir = lightVec / distance;
                    float attenuation = 1f / (distance * distance);
                    float radiusFactor = Hlsl.Saturate(1f - (distance / light.radius));
                    attenuation *= radiusFactor;

                    float NoL = Hlsl.Max(Hlsl.Dot(finalNormal, lightDir), 0f);
                    if (NoL <= 0f || attenuation <= 0f) continue;

                    float3 lightColor = light.color.RGB * light.intensity * attenuation;

                    if (shadowEffects.Y && distance < light.radius * 2f)
                    {
                        float3 shadowOrigin = hitPoint + geoNormal * EPSILON * REFLECTION_BIAS;
                        lightShadow = Hlsl.Min(SoftShadow2(shadowOrigin, lightDir,
                            shadowDistances.X, distance, out _), lightShadow);
                    }

                    float3 brdf = PBR.BRDFMicrofacetFunction(lightDir, viewDir,
                        finalNormal, baseCol, metallic, roughAlpha, specular, RECIPROCAL_PI, EPSILON);
                    totalLight += brdf * lightColor * NoL * lightShadow;
                }
            }

            return totalLight;
        }

        /// <summary>
        /// Calculates ambient occlusion by getting the distance to the second-closest SDF object.
        /// </summary>
        /// <param name="hitPoint">Initial hit point</param>
        /// <param name="normal">Initial hit normal</param>
        /// <param name="sdf">Initial sdf hit</param>
        /// <returns>Occlusion value at hit point</returns>
        private float CalculateAO(float3 hitPoint, float3 normal, uint entityId, float3 aoValues)
        {
            float3 samplePoint = hitPoint + normal * EPSILON; // Normal vector offset
            float worldDist = WorldSDF(samplePoint, false, entityId, out _); // Get distance to the next closest object
            float occlusionRadius = aoValues.Y;
            if (worldDist >= occlusionRadius) return 1f; // No object found within radius
            float occlusion = 1f - Hlsl.Saturate(worldDist / occlusionRadius);
            occlusion = Hlsl.Pow(occlusion, aoValues.Z);
            return 1f - occlusion;
        }

        #endregion lighting
        #region pbr_workflow

        private void SampleSurfaceMaterial(
            float3 hitPoint, float3 geoNormal, SDFObjectDTO sdf, float depth,
            out float3 localPos, out float3 blend, out float3 albedo,
            out float metallic, out float roughness, out float3 finalNormal)
        {
            localPos = ShaderMath.RotateVector(hitPoint - sdf.position, sdf.rotation);
            float3 localGeoNormal = Hlsl.Normalize(ShaderMath.RotateVector(geoNormal, sdf.rotation));
            blend = TriplanarWeights(localGeoNormal, 4f);
            float scale = 1f / Hlsl.Max(sdf.texTilingOffset.X, EPSILON);

            float mipLevel = 0f;
            if (sdf.albedoTexMetaID >= 0)
                mipLevel = EstimateMipLevel(depth, scale, textureMetadata[sdf.albedoTexMetaID].resolution);

            albedo = SampleTriplanarMip(sdf.albedoTexMetaID, localPos, blend, scale, mipLevel, sdf.color).RGB;
            metallic = SampleTriplanarScalarMip(sdf.metalTexMetaID, localPos, blend, scale, mipLevel, sdf.metallic);
            roughness = Hlsl.Max(SampleTriplanarScalarMip(sdf.roughTexMetaID, localPos, blend, scale, mipLevel, sdf.roughness), 0.045f);

            float3 localFinalNormal = SampleNormalTriplanarMip(sdf.normalTexMetaID, localPos, localGeoNormal, blend, scale, sdf.normalStrength, mipLevel);
            finalNormal = Hlsl.Normalize(ShaderMath.InverseRotateVector(localFinalNormal, sdf.rotation));
        }

        #endregion pbr_workflow
        #region raymarching

        // -----------
        // Raymarching
        // -----------

        //private float3 Raymarch(float3 rayOrigin, float3 rayDir, int maxSteps, float farClipPlane, out int closestObj, out float depth, out int steps)
        //{
        //    depth = worldData.nearPlane;
        //    closestObj = -1;
        //    float3 hitPoint = rayOrigin;

        //    for (steps = 0; steps < maxSteps && depth < farClipPlane; steps++)
        //    {
        //        hitPoint = rayOrigin + rayDir * depth;
        //        float worldDist = WorldSDF(hitPoint, false, uint.MaxValue, out closestObj);

        //        if (worldDist < AdaptiveEpsilon(depth)) break;
        //        depth += worldDist;
        //    }
        //    return hitPoint;
        //}

        private float3 Raymarch(float3 rayOrigin, float3 rayDir, int maxSteps,
            float farClipPlane, out int closestObj, out float depth, out int steps)
        {
            depth = worldData[0].nearPlane;
            closestObj = -1;
            float3 hitPoint = rayOrigin;
            float lastSafeDepth = depth;

            for (steps = 0; steps < maxSteps && depth < farClipPlane; steps++)
            {
                hitPoint = rayOrigin + rayDir * depth;
                float worldDist = WorldSDF(hitPoint, false, uint.MaxValue, out closestObj);

                if (worldDist > 0f) lastSafeDepth = depth;
                else if (worldDist < 0f)
                {
                    // Binary search, use fixed small epsilon, not adaptive
                    float tMin = lastSafeDepth;
                    float tMax = depth;
                    const float BISECT_EPS = EPSILON * 10f;

                    int maxRefine = (depth < 100f) ? 8 : 4;
                    for (int refine = 0; refine < maxRefine; refine++)
                    {
                        float tMid = (tMin + tMax) * 0.5f;
                        float3 refinePoint = rayOrigin + rayDir * tMid;
                        float refineDist = WorldSDF(refinePoint, false, uint.MaxValue, out int refinedObj);

                        if (Hlsl.Abs(refineDist) < BISECT_EPS)
                        {
                            depth = tMid;
                            closestObj = refinedObj;
                            return rayOrigin + rayDir * (tMid - BISECT_EPS); // Step back slightly to guarantee we're outside
                        }
                        if (refineDist > 0f)
                        {
                            tMin = tMid;
                            closestObj = refinedObj;
                        }
                        else tMax = tMid;
                    }

                    depth = tMin; // Always return the OUTSIDE point
                    return rayOrigin + rayDir * tMin;
                }

                // Use fixed epsilon near surface, adaptive only for early termination
                float hitEps = (depth < 10f) ? EPSILON : ShaderMath.AdaptiveEpsilon(depth, width, height, worldData[0].camScreenDist, EPSILON);
                if (worldDist < hitEps) break;

                depth += worldDist;
            }
            return hitPoint;
        }

        private static bool Refract(float3 incident, float3 normal, float eta, out float3 refracted)
        {
            float NdotI = Hlsl.Dot(normal, incident);
            float k = 1.0f - eta * eta * (1.0f - NdotI * NdotI);
            if (k < 0f)
            {
                refracted = float3.Zero;
                return false; // Total internal reflection
            }
            refracted = eta * incident - (eta * NdotI + Hlsl.Sqrt(k)) * normal;
            refracted = Hlsl.Normalize(refracted);
            return true;
        }

        private float3 TraceRefractionRay(float3 startDir, float3 startOrigin, float3 ambientBase, float3 backgroundCol,
            float3 normal, SDFObjectDTO startMat, out int steps)
        {
            float3 totalTransmittance = float3.One; // Start with full transmittance
            float3 accumulatedColor = float3.Zero;

            // Current state
            float3 currentOrigin = startOrigin;
            float3 currentDir = startDir;
            float3 currentNormal = normal;
            SDFObjectDTO currentMat = startMat;
            bool currentlyInsideObject = true;
            steps = 0;

            for (int transmit = 0; transmit < startMat.refractMaxRecursion; transmit++)
            {
                // Determine direction (into or out of material)
                float currentEta = currentlyInsideObject ? 1.0f / currentMat.ior : currentMat.ior;

                if (Refract(currentDir, currentNormal, currentEta, out float3 refractDir))
                {
                    // Trace through the current medium
                    float3 entryPt = currentOrigin - currentNormal * EPSILON * (currentlyInsideObject ? 1f : -1f);
                    float3 p = entryPt;
                    float travelDistance = 0f;
                    bool foundExit = false;
                    float3 exitPt = float3.Zero;
                    float3 exitNorm = float3.Zero;
                    float3 absorptionCoefficient = Hlsl.Log(Hlsl.Max(currentMat.absorptionColor.RGB, 0.001f));

                    for (int i = 0; i < currentMat.refractionMaxSteps; i++)
                    {
                        float d = WorldSDF(p, false, uint.MaxValue, out int closestObj);
                        bool nowInside = d < 0f;

                        if (currentlyInsideObject != nowInside)
                        {
                            // Binary search for the actual surface crossing
                            float3 tMin3 = p - refractDir * Hlsl.Max(Hlsl.Abs(d), 
                                ShaderMath.AdaptiveEpsilon(travelDistance, width, height, worldData[0].camScreenDist, EPSILON));
                            float3 tMax3 = p;
                            const float REFRACT_BISECT_EPS = EPSILON * 20f;

                            for (int b = 0; b < 12; b++)
                            {
                                float3 mid = (tMin3 + tMax3) * 0.5f;
                                float dMid = WorldSDF(mid, false, uint.MaxValue, out _);

                                if (Hlsl.Abs(dMid) < REFRACT_BISECT_EPS)
                                {
                                    tMin3 = mid; // close enough, use this
                                    break;
                                }

                                // tMin3 should always be on the "was inside" side
                                if ((dMid < 0f) == currentlyInsideObject) tMin3 = mid;
                                else tMax3 = mid;
                            }

                            // tMin3 is now on the side we WERE on — safe for normal sampling
                            exitPt = tMin3;
                            exitNorm = FastNormalSingleObject(exitPt, sdfObjects[closestObj]);
                            int exitClosestObj = closestObj;

                            if (Hlsl.Dot(exitNorm, refractDir) > 0f) exitNorm = -exitNorm;
                            foundExit = true;

                            float3 segmentTransmittance = Hlsl.Exp(
                                absorptionCoefficient * travelDistance * currentMat.absorptionColor.A * 5f);
                            totalTransmittance *= segmentTransmittance;

                            currentlyInsideObject = nowInside;
                            if (currentlyInsideObject && exitClosestObj != -1)
                            {
                                currentMat = sdfObjects[exitClosestObj];
                                absorptionCoefficient = Hlsl.Log(Hlsl.Max(currentMat.absorptionColor.RGB, 0.001f));
                            }
                            break;
                        }

                        float stepSize = Hlsl.Max(Hlsl.Abs(d), 
                            ShaderMath.AdaptiveEpsilon(travelDistance, width, height, worldData[0].camScreenDist, EPSILON));
                        p += refractDir * stepSize;
                        travelDistance += stepSize;
                    }

                    int tracedSteps;
                    if (!foundExit)
                    {
                        // Apply final absorption
                        float3 segmentTransmittance = Hlsl.Exp(absorptionCoefficient * travelDistance * currentMat.absorptionColor.A * 5f);
                        totalTransmittance *= segmentTransmittance;

                        float3 bgColor = TraceRefractionExitRay(p, refractDir, ambientBase, backgroundCol, out _, out _, out tracedSteps);
                        accumulatedColor = bgColor;
                        steps += tracedSteps;
                        break;
                    }

                    // Found exit
                    currentOrigin = exitPt;
                    currentDir = refractDir;
                    currentNormal = exitNorm;

                    if (!currentlyInsideObject) // If just exited to air
                    {
                        // Raymarch from exit point
                        float3 rayStart = exitPt + currentNormal * EPSILON;
                        float3 hitPoint = Raymarch(rayStart, currentDir, worldData[0].maxRaySteps, worldData[0].farPlane,
                            out int nextObjIndex, out _, out tracedSteps);
                        steps += tracedSteps;

                        if (nextObjIndex >= 0 && nextObjIndex < sdfObjects.Length)
                        {
                            currentOrigin = hitPoint;
                            currentNormal = FastNormal(hitPoint);
                            if (Hlsl.Dot(currentNormal, currentDir) > 0) currentNormal = -currentNormal;
                            currentMat = sdfObjects[nextObjIndex];
                            currentlyInsideObject = true;
                        }
                        else
                        {
                            float3 bgColor = TraceRefractionExitRay(rayStart, currentDir, ambientBase, backgroundCol, out _, out _, out tracedSteps);
                            accumulatedColor = bgColor;
                            steps += tracedSteps;
                            break;
                        }
                    }
                }
                else break; // Total internal reflection
            }

            return accumulatedColor * totalTransmittance;
        }

        private float3 TraceRefractionExitRay(float3 rayOrigin, float3 rayDir, float3 ambientBase, float3 backgroundCol,
            out float3 normal, out float totalDist, out int steps)
        {
            float3 finalColor = float3.Zero;
            normal = float3.Zero;

            // Trace
            int maxRaySteps = worldData[0].maxRaySteps;
            float3 hitPoint = Raymarch(rayOrigin, rayDir, maxRaySteps, worldData[0].farPlane, out int closestObjIndex, out totalDist, out steps);

            if (closestObjIndex == -1 || totalDist > worldData[0].farPlane)
            {
                finalColor += backgroundCol;
                return finalColor;
            }
            
            SDFObjectDTO sdf = sdfObjects[closestObjIndex];
            normal = FastNormal(hitPoint);
            float3 viewDir = -rayDir;

            // Sample the surface material for the sdf that was hit
            SampleSurfaceMaterial(hitPoint, normal, sdf, totalDist,
                out _, out _, out float3 albedo, out float metallic, out float roughness, out float3 finalNormal);
            float roughAlpha = roughness * roughness;

            // Calculate ambient occlusion for the hit point
            float aoValue = 1f;
            if (sdf.aoValues.X > 0f)
                aoValue = Hlsl.Lerp(1f, CalculateAO(hitPoint, normal, sdf.entityId, sdf.aoValues), sdf.aoValues.X);

            float3 directLight = CalculateLighting(hitPoint, normal, finalNormal, viewDir,
                sdf.shadowEffects, sdf.shadowDistances, sdf.specular, albedo, metallic, roughAlpha);
            float3 ambientLight = ambientBase * aoValue;
            finalColor += ambientLight + directLight;
            return finalColor;
        }

        /// <summary>
        /// Actually performs the main raymarching calculations.
        /// </summary>
        /// <param name="rayOrigin">Ray origin to start at</param>
        /// <param name="rayDir">Ray direction to travel</param>
        /// <param name="outputNormal">Outputs surface normal</param>
        /// <param name="totalDist">Total distance traversed</param> 
        /// <returns>Output raymarch color, with effects</returns>
        private float3 TraceRay(int2 pixel, float3 rayOrigin, float3 rayDir, int sampleIndexInPixel,
            out float3 outputNormal, out float totalDist, out int steps)
        {
            float3 finalColor = float3.Zero;
            float3 contribution = float3.One;
            float3 ambientBase = float3.Zero;
            float cosTheta = 0f;

            float3 refractedLight = float3.Zero;
            SDFObjectDTO mainMat = default;
            outputNormal = float3.Zero;
            totalDist = 0f;
            float farClipPlane = worldData[0].farPlane;
            bool firstHit = true;
            steps = 0;

            // Refraction coloring
            float3 surfaceColor = float3.Zero;
            float fresnelFactor = 0f;
            bool isRefractive = false;
            float aoValue = 1f;

            // Adaptive reflection step sizes
            int maxRaySteps = worldData[0].maxRaySteps;
            for (int bounce = 0; bounce < 32; bounce++)
            {
                float3 hitPoint = Raymarch(rayOrigin, rayDir, maxRaySteps, farClipPlane,
                    out int closestObjIndex, out float depth, out int tracedSteps);
                steps += tracedSteps;

                if (closestObjIndex == -1 || depth > farClipPlane)
                {
                    finalColor += contribution * worldData[0].backgroundColor.XYZ;
                    if (firstHit) totalDist = depth;
                    break;
                }

                SDFObjectDTO sdf = sdfObjects[closestObjIndex];
                float3 normal = FastNormal(hitPoint);
                if (Hlsl.Dot(normal, rayDir) > 0f) normal = -normal;
                float3 viewDir = -rayDir;

                SampleSurfaceMaterial(hitPoint, normal, sdf, depth,
                    out _, out _, out float3 albedo, out float metallic, out float roughness, out float3 finalNormal);
                float roughAlpha = roughness * roughness;

                float aoStrength = sdf.aoValues.X;
                cosTheta = Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f);

                if (firstHit)
                {
                    outputNormal = normal;
                    totalDist = depth;
                    entityIdBuffer[pixel.X + pixel.Y * (int)width] = new uint2((uint)closestObjIndex, sdf.entityId);
                    mainMat = sdf;

                    ambientBase = Hlsl.Lerp(albedo, worldData[0].backgroundColor.RGB, worldData[0].ambientStrength) * worldData[0].ambientStrength;
                    if (aoStrength > 0f)
                        aoValue = Hlsl.Lerp(1f, CalculateAO(hitPoint, normal, sdf.entityId, sdf.aoValues), aoStrength);

                    if (sdf.hasRefraction == 1)
                    {
                        isRefractive = true;
                        fresnelFactor = PBR.SimpleFresnelDielectric(cosTheta, mainMat.f0_dielectric);
                        refractedLight = TraceRefractionRay(rayDir, hitPoint, ambientBase, worldData[0].backgroundColor.RGB, normal, sdf, out tracedSteps);
                        steps += tracedSteps;
                    }
                    firstHit = false;
                }

                float3 directLight = CalculateLighting(hitPoint, normal, finalNormal, viewDir,
                    sdf.shadowEffects, sdf.shadowDistances, sdf.specular, albedo, metallic, roughAlpha);
                if (Hlsl.Length(directLight) > 0.001f) aoValue = 1f;
                float3 ambientLight = ambientBase * aoValue;

                if (bounce == 0) surfaceColor = directLight;
                finalColor += contribution * (ambientLight + directLight);

                if (sdf.hasReflection == 0) break;
                if (bounce == sdf.reflectionMaxBounces - 1) break;

                maxRaySteps = (int)(maxRaySteps / sdf.reflectRayStepFalloff);

                // Metallic/roughness maps now actually drive this:
                float3 f0Refl = Hlsl.Lerp(sdf.f0_reflectance, albedo, new float3(metallic, metallic, metallic));
                float3 F = PBR.FresnelSchlick(Hlsl.Max(Hlsl.Dot(finalNormal, viewDir), 0f), f0Refl);
                float reflectionChance = Hlsl.Lerp(F.X, 1f, metallic);

                if (bounce == 0 && isRefractive) reflectionChance = fresnelFactor;
                if (reflectionChance < MIN_REFLECTION_CHANCE) break;

                contribution *= F * (1f - roughness * 0.5f);
                contribution = Hlsl.Min(contribution, 10f);
                if (contribution.X + contribution.Y + contribution.Z < MIN_THROUGHPUT) break;

                int reflectionSampleIndex = pixel.X * 73 + pixel.Y * 9277 + frameCount * 1973 + sampleIndexInPixel * 3271 + bounce * 997;
                float2 u = PBR.Halton2D(reflectionSampleIndex);
                // Perturb with the bumped normal, not the geometric one -> normal maps now visible in reflections
                float3 halfVector = PBR.ImportanceSampleGGX(u, finalNormal, roughAlpha, PI);
                float3 reflectDir = Hlsl.Normalize(Hlsl.Reflect(rayDir, halfVector));
                if (Hlsl.Dot(reflectDir, normal) < 0.01f) break; // test vs geometric normal, avoids self-intersection

                rayOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS; // offset along geometric normal, stays safe
                rayDir = reflectDir;
            }

            float3 outputColor;
            if (isRefractive)
            {
                float fresnel = Hlsl.Lerp(0.04f, 1.0f, Hlsl.Pow(1.0f - cosTheta, mainMat.reflectance));
                float3 reflectiveCol = finalColor + surfaceColor;
                outputColor = reflectiveCol * fresnel + refractedLight * (1f - fresnel);
            }
            else outputColor = finalColor + surfaceColor;

            return outputColor;
        }

        #endregion raymarching
        #region debug

        private float3 DebugBRDF(int2 pixel, float3 rayDir, float3 hitPoint, float depth)
        {
            float3 outputVec = float3.Zero;
            uint2 idData = entityIdBuffer[pixel.X + pixel.Y * (int)width];
            if (idData.X != uint.MaxValue)
            {
                SDFObjectDTO sdf = sdfObjects[(int)idData.X];
                float3 normal = depthNormals[pixel].GBA;
                float3 viewDir = -rayDir;

                if (Hlsl.Length(worldData[0].mainLightDir) > 0f)
                {
                    SampleSurfaceMaterial(hitPoint, normal, sdf, depth,
                        out _, out _, out float3 albedo, out float metallic, out float roughness, out float3 finalNormal);
                    float roughAlpha = roughness * roughness;

                    float NoL = Hlsl.Max(Hlsl.Dot(finalNormal, worldData[0].mainLightDir), 0f);
                    if (NoL > 0f)
                    {
                        if (debugMode == 6)
                            outputVec = PBR.BRDFMicrofacetFunction(worldData[0].mainLightDir, viewDir,
                                finalNormal, albedo, metallic, roughAlpha, sdf.specular, RECIPROCAL_PI, EPSILON);
                        else if (debugMode == 7)
                        {
                            float3 halfwayDir = Hlsl.Normalize(viewDir + worldData[0].mainLightDir);
                            float NoV = Hlsl.Saturate(Hlsl.Dot(finalNormal, viewDir));
                            float NoH = Hlsl.Saturate(Hlsl.Dot(finalNormal, halfwayDir));
                            float VoH = Hlsl.Saturate(Hlsl.Dot(viewDir, halfwayDir));
                            float3 f0 = Hlsl.Lerp(float3.One * 0.16f * sdf.specular * sdf.specular, albedo, new float3(metallic, metallic, metallic));
                            float3 F = PBR.FresnelSchlick(VoH, f0);
                            float D = PBR.D_GGX(NoH, roughAlpha, RECIPROCAL_PI);
                            float G = PBR.GSmith(NoV, NoL, roughAlpha, EPSILON);
                            outputVec = F * D * G / (4f * Hlsl.Max(NoV * NoL, EPSILON));
                        }
                        else if (debugMode == 8)
                        {
                            float NoV = Hlsl.Saturate(Hlsl.Dot(finalNormal, viewDir));
                            float VoH = Hlsl.Saturate(Hlsl.Dot(viewDir, Hlsl.Normalize(viewDir + worldData[0].mainLightDir)));
                            outputVec = albedo * PBR.DisneyDiffuseFactor(NoV, NoL, VoH, roughAlpha) * RECIPROCAL_PI;
                        }
                    }
                }
            }
            return outputVec;
            //float3 H = Hlsl.Normalize(V + L);
            //float NdotV = Hlsl.Max(Hlsl.Dot(N, V), 0.0f);
            //float NdotL = Hlsl.Max(Hlsl.Dot(N, L), 0.0f);
            //float NdotH = Hlsl.Max(Hlsl.Dot(N, H), 0.0f);

            //float D = D_GGX(NdotH, roughness);
            //float G = G1_GGX_Schlick(NdotV, roughness) * G1_GGX_Schlick(NdotL, roughness);

            //float3 f0 = float3.One * 0.16f * reflectance * reflectance;
            //float3 F = FresnelSchlick(Hlsl.Max(Hlsl.Dot(V, H), 0.0f), f0);

            //// Return RGB with R = D, G = G, B = average(F)
            //return new float3(D, G, (F.X + F.Y + F.Z) / 3.0f);
        }

        /// <summary>
        /// Maps a continuous mip level to a distinct discrete color band for debug visualization.
        /// White = mip 0 (full res), progressing through the spectrum as mip level rises,
        /// dark gray for anything past what's realistically expected.
        /// </summary>
        private static float3 MipLevelColor(float mipLevel)
        {
            int level = (int)Hlsl.Floor(mipLevel + 0.5f); // round to nearest whole mip for clean bands
            if (level <= 0) return new float3(1.0f, 1.0f, 1.0f);       // mip 0: white
            else if (level == 1) return new float3(0.0f, 1.0f, 0.0f);  // mip 1: green
            else if (level == 2) return new float3(0.0f, 1.0f, 1.0f);  // mip 2: cyan
            else if (level == 3) return new float3(0.0f, 0.0f, 1.0f);  // mip 3: blue
            else if (level == 4) return new float3(1.0f, 0.0f, 1.0f);  // mip 4: magenta
            else if (level == 5) return new float3(1.0f, 0.0f, 0.0f);  // mip 5: red
            else if (level == 6) return new float3(1.0f, 0.5f, 0.0f);  // mip 6: orange
            else if (level == 7) return new float3(1.0f, 1.0f, 0.0f);  // mip 7: yellow
            else return new float3(0.3f, 0.3f, 0.3f);                  // mip 8+: dark gray
        }

        #endregion debug
        #region main

        /// <summary>
        /// Executes the raymarching sequence.
        /// </summary>
        public void Execute()
        {
            int2 localPixel = ThreadIds.XY;
            int2 pixel;
            if (enableCheckerboard == 1) // Checkerboard pattern
            {
                int framePass = frameCount & 1;
                pixel = new int2(
                    localPixel.X * 2 + (framePass ^ (localPixel.Y & 1)),
                    localPixel.Y
                );
            }
            else pixel = localPixel; // Full resolution rendering
            if (pixel.X >= width || pixel.Y >= height) return;

            texture[pixel] = new float4(0, 0, 0, 0);
            depthNormals[pixel] = new float4(0, 0, 0, 0);
            entityIdBuffer[pixel.X + pixel.Y * (int)width] = new uint2(uint.MaxValue, uint.MaxValue);

            float2 uv = (float2)pixel / new float2(width, height) * 2f - 1f; // UV math
            float3 rayOrigin = worldData[0].cameraOrigin;

            float3 accumulatedColor = float3.Zero;
            float3 accumulatedNormal = float3.Zero;
            float3 rayDir = float3.Zero;
            float accumulatedDistance = 0f;
            int steps = 0;

            // Test background image
            //float3 backgroundCol = inputTestTexture[new int2((int)(uv.X * backgroundWidth), (int)(uv.Y * backgroundHeight))].RGB;

            for (int sample = 0; sample < SAMPLES_PER_PIXEL; sample++)
            {
                // Add slight jitter using Halton for antialiasing
                if (SAMPLES_PER_PIXEL > 1)
                {
                    int cameraSampleIndex = pixel.X * 73 + pixel.Y * 9277 + frameCount * 1973 + sample;
                    float2 jitter = (PBR.Halton2D(cameraSampleIndex) - 0.5f) / new float2(width, height);
                    float2 jitteredUV = uv + jitter * 2f;
                    rayDir = ShaderMath.GetCameraRayDirNew(aspect, jitteredUV,
                        worldData[0].camScreenDist, worldData[0].camForward, worldData[0].camRight, worldData[0].camUp);
                }
                //else
                //{
                //    rayDir = GetCameraRayDirNew(uv);
                //}

                // Automatically skip reflection bounces for non-reflective materials
                float3 color = TraceRay(pixel, rayOrigin, rayDir, sample,
                    out float3 outputNormal, out float dist, out int tracedSteps);
                steps += tracedSteps;
                accumulatedColor += color;
                accumulatedNormal += outputNormal;
                accumulatedDistance += dist;
            }

            float maxPossibleDistance = worldData[0].farPlane - worldData[0].nearPlane;
            float3 finalColor = accumulatedColor / SAMPLES_PER_PIXEL;
            float3 finalNormal = accumulatedNormal / SAMPLES_PER_PIXEL;
            float finalDist = accumulatedDistance / SAMPLES_PER_PIXEL;

            // Optional ACES:
            finalColor = Hlsl.Saturate(finalColor * (2.51f * finalColor + 0.03f) / (finalColor * (2.43f * finalColor + 0.59f) + 0.14f));

            // Final
            texture[pixel] = new float4(finalColor, 1f);
            depthNormals[pixel] = new float4(finalDist / maxPossibleDistance, finalNormal);

            // Debugging:
            if (debugMode > 0)
            {
                if (debugMode == 1) // Depth buffer
                {
                    float depth = depthNormals[pixel].R;
                    texture[pixel] = new float4(depth, depth, depth, 1);
                }
                else if (debugMode == 2) // World normal buffer
                    texture[pixel] = new float4(depthNormals[pixel].GBA, 1);
                else if (debugMode == 3) // Object ID buffer
                {
                    float3 objColor = ShaderMath.IntToColor(entityIdBuffer[pixel.X + pixel.Y * (int)width].X);
                    texture[pixel] = new float4(objColor, 1);
                }
                else if (debugMode == 4) // Ray steps
                {
                    float stepValue1 = Hlsl.Saturate((float)steps / worldData[0].maxRaySteps);
                    float stepValue2 = Hlsl.Saturate((float)(steps - worldData[0].maxRaySteps) / worldData[0].maxRaySteps);
                    float stepValue3 = Hlsl.Saturate((float)(steps - worldData[0].maxRaySteps * 2) / worldData[0].maxRaySteps);
                    texture[pixel] = new float4(stepValue1, stepValue2, stepValue3, 1);
                }
                else if (debugMode == 5) // Shadows
                {
                    float3 shadowColor = new float3(1, 0, 0);

                    uint2 idData = entityIdBuffer[pixel.X + pixel.Y * (int)width];
                    if (idData.X != uint.MaxValue)
                    {
                        SDFObjectDTO sdf = sdfObjects[(int)idData.X];
                        float3 hitPoint = rayOrigin + rayDir * depthNormals[pixel].R * (worldData[0].farPlane - worldData[0].nearPlane);
                        float3 normal = depthNormals[pixel].GBA;

                        if (Hlsl.Length(worldData[0].mainLightDir) > 0f)
                        {
                            float3 shadowOrigin = hitPoint + normal * EPSILON * REFLECTION_BIAS;
                            float shadow = SoftShadow2(shadowOrigin, worldData[0].mainLightDir,
                                sdf.shadowDistances.X, sdf.shadowDistances.Y, out int shadowObj);

                            float3 occluderColor = float3.Zero;
                            if (shadowObj >= 0 && shadowObj < sdfObjects.Length)
                                occluderColor = ShaderMath.IntToColor(sdfObjects[shadowObj].entityId);

                            shadowColor = new float3(shadow, occluderColor.G, occluderColor.B);
                        }
                    }
                    texture[pixel] = new float4(shadowColor, 1);
                }
                else if (debugMode == 6 || debugMode == 7 || debugMode == 8) // BRDF, Specular, Diffuse
                {
                    float3 objColor = new float3(1, 0, 1);
                    uint2 idData = entityIdBuffer[pixel.X + pixel.Y * (int)width];
                    if (idData.X != uint.MaxValue)
                    {
                        float depth = depthNormals[pixel].R;
                        float3 hitPoint = rayOrigin + rayDir * depth * (worldData[0].farPlane - worldData[0].nearPlane);
                        objColor = DebugBRDF(pixel, rayDir, hitPoint, depth);
                    }
                    texture[pixel] = new float4(objColor, 1);
                }
                else if (debugMode == 9) // Mip cascades
                {
                    float3 objColor = new float3(0, 0, 0);
                    uint2 idData = entityIdBuffer[pixel.X + pixel.Y * (int)width];
                    if (idData.X != uint.MaxValue)
                    {
                        SDFObjectDTO sdf = sdfObjects[(int)idData.X];
                        if (sdf.albedoTexMetaID >= 0)
                        {
                            float depth = depthNormals[pixel].R * (worldData[0].farPlane - worldData[0].nearPlane);
                            float scale = 1f / Hlsl.Max(sdf.texTilingOffset.X, EPSILON);
                            float mipLevel = EstimateMipLevel(depth, scale, textureMetadata[sdf.albedoTexMetaID].resolution);
                            objColor = MipLevelColor(mipLevel);
                        }
                    }
                    texture[pixel] = new float4(objColor, 1);
                }
            }
        }

        #endregion main
    }
}

// ----------------------------
// Functions and code obseleted
// ----------------------------

/* Calculates shadows
// Adapted: https://www.shadertoy.com/view/lsKcDD
private float SoftShadow(float3 rayOrigin, float3 rayDir, float minDist, float maxDist)
{
    float res = 1.0f;
    float rayDist = minDist;

    for (int i = 0; i < 100 && rayDist < maxDist; i++)
    {
        float sceneSDF = WorldSDF(rayOrigin + rayDist * rayDir, true).X;
        res = Hlsl.Min(res, sceneSDF / (0.5f * rayDist));
        rayDist += Hlsl.Clamp(sceneSDF, 0.005f, 0.05f);

        if (res < -1.0f || rayDist > maxDist)
            break;
    }

    res = Hlsl.Max(res, -1.0f);
    return 0.25f * (1.0f + res) * (1.0f + res) * (2.0f - res);
}*/

/*private float2 SoftShadowCambridge(float3 lightPos, float3 hitPoint, float renderDepth)
{
    float3 lightDir = Hlsl.Normalize(lightPos - hitPoint);
    float kd = 1f;
    float lastObj = -1;
    int step = 0;
    for (float t = 0.1f; t < Hlsl.Length(lightPos - hitPoint) && step < renderDepth && kd > 0.001f; )
    {
        float2 worldSDF = WorldSDF(hitPoint + t * lightDir, true);
        lastObj = worldSDF.Y;
        float d = Hlsl.Abs(worldSDF.X);
        if (d < 0.001f)
        {
            kd = 0;
        }
        else
        {
            kd = Hlsl.Min(kd, 16 * d / t);
        }
        t += d;
        step++;
    }
    return new float2(kd, lastObj);
}*/

/*private float3 RIS_SampleReflection(
    int2 pixel,
    float3 hitPoint,
    float3 normal,
    float3 viewDir,
    float roughness,
    float metallic,
    float3 f0,
    int frameCount,
    int bounce,
    out float misWeight)
{
    // Reservoir for RIS
    Reservoir reservoir = new Reservoir
    {
        sumWeights = 0f,
        M = 0,
        sampleDirection = float3.Zero,
        sourcePDF = 0f,
        targetPDF = 0f
    };

    const int M_CANDIDATES = 32;  // Generate 32 candidates
    float alpha = roughness * roughness;

    for (int i = 0; i < M_CANDIDATES; i++)
    {
        // Get unique seed for this candidate
        uint seed = GetSeed(pixel, i, bounce, frameCount);

        // Generate candidate using GGX importance sampling
        float2 u = Halton2DScrambled(i, seed);
        float3 candidateDir = ImportanceSampleGGX(u, normal, roughness);

        // Ensure candidate is above surface
        float NdotL = Hlsl.Max(Hlsl.Dot(normal, candidateDir), 0f);
        if (NdotL < 0.001f) continue;

        // Evaluate source PDF (BRDF PDF)
        float3 H = Hlsl.Normalize(viewDir + candidateDir);
        float NoH = Hlsl.Max(Hlsl.Dot(normal, H), 0f);
        float VoH = Hlsl.Max(Hlsl.Dot(viewDir, H), 0f);

        // GGX PDF
        float D = D_GGX(NoH, roughness);
        float sourcePDF = D * NoH / (4.0f * VoH);

        if (sourcePDF < 1e-6f) continue;

        // Estimate incoming radiance for target PDF
        // Simple approximation: could be improved with radiance cache
        float estimatedRadiance = 1.0f;  // Placeholder - you'll improve this

        // For now, use BRDF value as target PDF
        float3 F = FresnelSchlick(VoH, f0);
        float G = GSmith(Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f),
                        NdotL, roughness);

        float3 brdfValue = F * D * G / (4.0f * NdotL * Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f));
        float targetPDF = Hlsl.Length(brdfValue) * estimatedRadiance * NdotL;

        // Get random for reservoir update
        float random = ScrambledHalton(i, 5, seed) % 1.0f;

        // Update reservoir
        reservoir = UpdateReservoir(reservoir, candidateDir, sourcePDF, targetPDF, random);
    }

    // Calculate MIS weight
    float misWeight = 1.0f;
    if (reservoir.M > 0 && reservoir.sumWeights > 0f && reservoir.sourcePDF > 0f)
    {
        misWeight = reservoir.targetPDF / (reservoir.sourcePDF * reservoir.sumWeights / reservoir.M);
    }

    return (reservoir.sampleDirection, float3.One, misWeight);
}*/

// Old lighting
//float3 ambientLightAmt = ambientBase * ao;
//float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
//float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal, metallic, roughness * roughness, albedoColor, specular);
//float3 directLight = Hlsl.Lerp(ambientLightAmt, brdf, /*shadowValue * */NoL);

// Old direct lighting
//float NoL = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0f);
//float3 brdf = BRDFMicrofacetFunction(lightDir, viewDir, normal, metallic, roughness * roughness, albedoColor, specular);
//float3 directLight = Hlsl.Lerp(ambientBase * ao, brdf, shadowValue * NoL);

#pragma warning restore CA1416 // Validate platform compatibility