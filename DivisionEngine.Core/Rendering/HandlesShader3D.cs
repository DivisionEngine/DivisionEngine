//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//

//using ComputeSharp;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;

//namespace DivisionEngine.Rendering
//{
//    [GeneratedComputeShaderDescriptor]
//    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
//    public readonly partial struct HandlesShader3D(
//        float width,
//        float height,
//        float aspect,
//        SDFWorldDTO worldData,
//        ReadWriteTexture2D<float4> colorTexture,
//        ReadWriteTexture2D<float4> depthNormalsTexture,
//        ReadWriteBuffer<uint2> entityIdBuffer,
//        ReadOnlyBuffer<HandleDTO> handles,
//        ReadOnlyTexture2D<float4> iconAtlas) : IComputeShader
//    {
//        private const float EPSILON = 0.0001f;
//        private const float HANDLE_DEPTH_BIAS = 0.001f; // Slight bias to prevent z-fighting

//        /// <summary>
//        /// Gets camera ray direction for screen coordinates
//        /// </summary>
//        private float3 GetCameraRayDir(float2 uv)
//        {
//            float px = uv.X * aspect * worldData.camScreenDist;
//            float py = uv.Y * worldData.camScreenDist;
//            float3 rayDir = worldData.camForward + worldData.camRight * px + worldData.camUp * py;
//            return Hlsl.Normalize(rayDir);
//        }

//        /// <summary>
//        /// Projects world position to screen space
//        /// </summary>
//        private float2 WorldToScreen(float3 worldPos)
//        {
//            float3 toPoint = worldPos - worldData.cameraOrigin;
//            float3 cameraForward = worldData.camForward;
//            float3 cameraRight = worldData.camRight;
//            float3 cameraUp = worldData.camUp;

//            float dist = Hlsl.Dot(toPoint, cameraForward);
//            if (dist <= 0.001f) return new float2(-2f, -2f); // Behind camera

//            float3 projected = new float3(
//                Hlsl.Dot(toPoint, cameraRight),
//                Hlsl.Dot(toPoint, cameraUp),
//                dist
//            );

//            float2 screenPos = new float2(
//                projected.X / (projected.Z * worldData.camScreenDist),
//                projected.Y / (projected.Z * worldData.camScreenDist)
//            );

//            // Convert from NDC [-1,1] to UV [0,1]
//            screenPos = screenPos * 0.5f + 0.5f;
//            return screenPos;
//        }

//        /// <summary>
//        /// Gets screen space depth of a world position
//        /// </summary>
//        private float GetScreenDepth(float3 worldPos)
//        {
//            float3 toPoint = worldPos - worldData.cameraOrigin;
//            float dist = Hlsl.Dot(toPoint, worldData.camForward);
//            float maxDist = worldData.farPlane - worldData.nearPlane;
//            return Hlsl.Saturate(dist / maxDist);
//        }

//        /// <summary>
//        /// Checks if a point is occluded by scene geometry
//        /// </summary>
//        private bool IsOccluded(float3 worldPos, float2 screenPos)
//        {
//            int2 pixel = (int2)(screenPos * new float2(width, height));
//            if (pixel.X < 0 || pixel.X >= width || pixel.Y < 0 || pixel.Y >= height)
//                return false;

//            float handleDepth = GetScreenDepth(worldPos);
//            float sceneDepth = depthNormalsTexture[pixel].X;

//            // Handle is occluded if it's behind scene geometry
//            return handleDepth > sceneDepth + HANDLE_DEPTH_BIAS;
//        }

//        /// <summary>
//        /// Draws a line between two points
//        /// </summary>
//        private float4 DrawLine(float3 start, float3 end, float4 color, float thickness, float2 uv, float2 screenPos, float3 rayOrigin, float3 rayDir)
//        {
//            // Project both endpoints to screen space
//            float2 screenStart = WorldToScreen(start);
//            float2 screenEnd = WorldToScreen(end);

//            // Check if line is behind camera
//            if (screenStart.X < -1f || screenEnd.X < -1f) return float4.Zero;

//            // Calculate distance from pixel to line segment
//            float2 pixelPos = uv * new float2(width, height);
//            float2 startPx = screenStart * new float2(width, height);
//            float2 endPx = screenEnd * new float2(width, height);

//            float2 lineVec = endPx - startPx;
//            float2 pixelVec = pixelPos - startPx;

//            float lineLength = Hlsl.Length(lineVec);
//            if (lineLength < EPSILON) return float4.Zero;

//            float t = Hlsl.Clamp(Hlsl.Dot(pixelVec, lineVec) / (lineLength * lineLength), 0f, 1f);
//            float2 closestPoint = startPx + lineVec * t;
//            float dist = Hlsl.Length(pixelPos - closestPoint);

//            if (dist > thickness) return float4.Zero;

//            // Calculate alpha with anti-aliasing
//            float alpha = Hlsl.SmoothStep(thickness, thickness - 1f, dist);

//            // Check if line is occluded (check both endpoints)
//            bool isOccluded = IsOccluded(start, screenStart) && IsOccluded(end, screenEnd);
//            if (isOccluded) alpha *= 0.5f; // Fade occluded handles

//            return new float4(color.RGB, color.A * alpha);
//        }

//        /// <summary>
//        /// Draws an arrow (line with triangle at end)
//        /// </summary>
//        private float4 DrawArrow(float3 position, float3 direction, float4 color, float size, float2 uv, float2 screenPos, float3 rayOrigin, float3 rayDir)
//        {
//            float3 normDir = Hlsl.Normalize(direction);
//            float3 start = position;
//            float3 end = position + normDir * size;

//            // Draw the line
//            float4 result = DrawLine(start, end, color, size * 0.1f, uv, screenPos, rayOrigin, rayDir);

//            // Draw arrow head as three lines
//            float headSize = size * 0.2f;
//            float3 right = Hlsl.Normalize(Hlsl.Cross(normDir, worldData.camUp));
//            float3 up = Hlsl.Normalize(Hlsl.Cross(right, normDir));

//            float3 headBase = end - normDir * headSize;
//            float3 headPoint1 = headBase + right * headSize * 0.5f + up * headSize * 0.5f;
//            float3 headPoint2 = headBase - right * headSize * 0.5f + up * headSize * 0.5f;
//            float3 headPoint3 = headBase + up * headSize;

//            result += DrawLine(end, headPoint1, color, size * 0.1f, uv, screenPos, rayOrigin, rayDir);
//            result += DrawLine(end, headPoint2, color, size * 0.1f, uv, screenPos, rayOrigin, rayDir);
//            result += DrawLine(headPoint1, headPoint3, color, size * 0.1f, uv, screenPos, rayOrigin, rayDir);
//            result += DrawLine(headPoint2, headPoint3, color, size * 0.1f, uv, screenPos, rayOrigin, rayDir);

//            return result;
//        }

//        /// <summary>
//        /// Draws a circle in world space
//        /// </summary>
//        private float4 DrawCircle(float3 center, float radius, float4 color, float thickness, float2 uv, float2 screenPos, float3 rayOrigin, float3 rayDir)
//        {
//            // For circles, we'll ray-trace a disc aligned with camera view
//            float3 normal = Hlsl.Normalize(center - worldData.cameraOrigin);
//            float3 right = Hlsl.Normalize(Hlsl.Cross(normal, worldData.camUp));
//            float3 up = Hlsl.Normalize(Hlsl.Cross(right, normal));

//            // Ray-plane intersection
//            float denom = Hlsl.Dot(rayDir, normal);
//            if (Hlsl.Abs(denom) < EPSILON) return float4.Zero;

//            float t = Hlsl.Dot(center - rayOrigin, normal) / denom;
//            if (t < 0f) return float4.Zero;

//            float3 hitPoint = rayOrigin + rayDir * t;
//            float3 toHit = hitPoint - center;
//            float distToCenter = Hlsl.Length(toHit);

//            // Check if point is on circle circumference
//            float delta = Hlsl.Abs(distToCenter - radius);
//            if (delta > thickness) return float4.Zero;

//            // Check occlusion
//            float2 hitScreen = WorldToScreen(hitPoint);
//            bool isOccluded = IsOccluded(hitPoint, hitScreen);

//            float alpha = Hlsl.SmoothStep(thickness, thickness - 1f, delta);
//            if (isOccluded) alpha *= 0.5f;

//            return new float4(color.RGB, color.A * alpha);
//        }

//        /// <summary>
//        /// Draws a sphere handle (like for point lights)
//        /// </summary>
//        private float4 DrawSphere(float3 center, float radius, float4 color, float2 uv, float2 screenPos, float3 rayOrigin, float3 rayDir)
//        {
//            // Ray-sphere intersection
//            float3 oc = rayOrigin - center;
//            float b = 2f * Hlsl.Dot(oc, rayDir);
//            float c = Hlsl.Dot(oc, oc) - radius * radius;
//            float discriminant = b * b - 4f * c;

//            if (discriminant < 0f) return float4.Zero;

//            float sqrtD = Hlsl.Sqrt(discriminant);
//            float t1 = (-b - sqrtD) * 0.5f;
//            float t2 = (-b + sqrtD) * 0.5f;

//            float t = t1 > 0f ? t1 : t2;
//            if (t <= 0f) return float4.Zero;

//            float3 hitPoint = rayOrigin + rayDir * t;
//            float3 normal = Hlsl.Normalize(hitPoint - center);

//            // Simple lighting for handle sphere
//            float3 lightDir = Hlsl.Normalize(worldData.mainLightDir);
//            float ndotl = Hlsl.Max(Hlsl.Dot(normal, lightDir), 0.2f);
//            float3 litColor = color.RGB * ndotl;

//            // Add rim highlight
//            float3 viewDir = -rayDir;
//            float rim = Hlsl.Pow(1f - Hlsl.Max(Hlsl.Dot(normal, viewDir), 0f), 2f);
//            litColor += color.RGB * rim * 0.5f;

//            // Check occlusion
//            float2 hitScreen = WorldToScreen(hitPoint);
//            bool isOccluded = IsOccluded(hitPoint, hitScreen);
//            float alpha = color.A;
//            if (isOccluded) alpha *= 0.5f;

//            return new float4(litColor, alpha);
//        }

//        /// <summary>
//        /// Draws a cube handle (like for directional lights)
//        /// </summary>
//        private float4 DrawCube(float3 center, float3 size, float4 color, float3 rayOrigin, float3 rayDir)
//        {
//            // Ray-box intersection using slab method
//            float3 invDir = 1f / rayDir;
//            float3 tMin = (center - size * 0.5f - rayOrigin) * invDir;
//            float3 tMax = (center + size * 0.5f - rayOrigin) * invDir;

//            float3 t1 = Hlsl.Min(tMin, tMax);
//            float3 t2 = Hlsl.Max(tMin, tMax);

//            float tNear = Hlsl.Max(Hlsl.Max(t1.X, t1.Y), t1.Z);
//            float tFar = Hlsl.Min(Hlsl.Min(t2.X, t2.Y), t2.Z);

//            if (tNear > tFar || tFar < 0f) return float4.Zero;

//            float t = tNear > 0f ? tNear : tFar;
//            if (t <= 0f) return float4.Zero;

//            float3 hitPoint = rayOrigin + rayDir * t;

//            // Simple wireframe effect
//            float3 localPos = hitPoint - center;
//            float3 halfSize = size * 0.5f;
//            float3 delta = halfSize - Hlsl.Abs(localPos);
//            float minDelta = Hlsl.Min(Hlsl.Min(delta.X, delta.Y), delta.Z);

//            float edgeWidth = 0.05f;
//            if (minDelta > edgeWidth) return float4.Zero;

//            float alpha = Hlsl.SmoothStep(edgeWidth, edgeWidth * 0.5f, minDelta);

//            float2 hitScreen = WorldToScreen(hitPoint);
//            bool isOccluded = IsOccluded(hitPoint, hitScreen);
//            if (isOccluded) alpha *= 0.5f;

//            return new float4(color.RGB, color.A * alpha);
//        }

//        /// <summary>
//        /// Draws an icon (like for lights and cameras)
//        /// </summary>
//        private float4 DrawIcon(float3 position, float4 color, float size, float4 iconUV, float2 uv, float2 screenPos, float3 rayOrigin, float3 rayDir)
//        {
//            // Always face camera (billboard)
//            float3 toCamera = worldData.cameraOrigin - position;
//            float dist = Hlsl.Length(toCamera);
//            if (dist < EPSILON) return float4.Zero;

//            float3 forward = Hlsl.Normalize(toCamera);
//            float3 right = Hlsl.Normalize(Hlsl.Cross(forward, worldData.camUp));
//            float3 up = Hlsl.Normalize(Hlsl.Cross(right, forward));

//            // Calculate world space quad corners
//            float halfSize = size * 0.5f;
//            float3 corners[4] = {
//                position - right * halfSize - up * halfSize,
//                position + right * halfSize - up * halfSize,
//                position - right * halfSize + up * halfSize,
//                position + right * halfSize + up * halfSize
//            };

//            // Check if any corner is behind camera
//            for (int i = 0; i < 4; i++)
//            {
//                float3 toCorner = corners[i] - worldData.cameraOrigin;
//                if (Hlsl.Dot(toCorner, worldData.camForward) <= 0f)
//                    return float4.Zero;
//            }

//            // Simple quad ray intersection
//            float3 normal = forward;
//            float denom = Hlsl.Dot(rayDir, normal);
//            if (Hlsl.Abs(denom) < EPSILON) return float4.Zero;

//            float t = Hlsl.Dot(position - rayOrigin, normal) / denom;
//            if (t < 0f) return float4.Zero;

//            float3 hitPoint = rayOrigin + rayDir * t;

//            // Get UV coordinates on quad
//            float3 toHit = hitPoint - position;
//            float u = Hlsl.Dot(toHit, right) / size + 0.5f;
//            float v = Hlsl.Dot(toHit, up) / size + 0.5f;

//            if (u < 0f || u > 1f || v < 0f || v > 1f) return float4.Zero;

//            // Sample icon atlas
//            float2 sampleUV = iconUV.XY + new float2(u, v) * (iconUV.ZW - iconUV.XY);
//            float4 iconColor = iconAtlas.SampleLevel(sampleUV, 0);

//            // Check occlusion
//            bool isOccluded = IsOccluded(hitPoint, screenPos);
//            float alpha = iconColor.A * color.A;
//            if (isOccluded) alpha *= 0.5f;

//            return new float4(iconColor.RGB * color.RGB, alpha);
//        }

//        /// <summary>
//        /// Draws transform gizmo (translate, rotate, scale)
//        /// </summary>
//        private float4 DrawTransformGizmo(float3 position, float4 rotation, float size, float2 uv, float2 screenPos, float3 rayOrigin, float3 rayDir)
//        {
//            float4 result = float4.Zero;
//            float3 xAxis = new float3(1, 0, 0);
//            float3 yAxis = new float3(0, 1, 0);
//            float3 zAxis = new float3(0, 0, 1);

//            // Rotate axes by handle rotation
//            xAxis = RotateVector(xAxis, rotation);
//            yAxis = RotateVector(yAxis, rotation);
//            zAxis = RotateVector(zAxis, rotation);

//            // Draw X axis (red)
//            result += DrawArrow(position, xAxis, new float4(1, 0, 0, 1), size, uv, screenPos, rayOrigin, rayDir);
//            // Draw Y axis (green)
//            result += DrawArrow(position, yAxis, new float4(0, 1, 0, 1), size, uv, screenPos, rayOrigin, rayDir);
//            // Draw Z axis (blue)
//            result += DrawArrow(position, zAxis, new float4(0, 0, 1, 1), size, uv, screenPos, rayOrigin, rayDir);

//            // Draw sphere at center
//            result += DrawSphere(position, size * 0.1f, new float4(0.5f, 0.5f, 0.5f, 1), uv, screenPos, rayOrigin, rayDir);

//            return result;
//        }

//        /// <summary>
//        /// Helper: Rotate vector by quaternion
//        /// </summary>
//        private float3 RotateVector(float3 v, float4 q)
//        {
//            float3 qv = q.XYZ;
//            float3 t = 2f * Hlsl.Cross(qv, v);
//            return v + q.W * t + Hlsl.Cross(qv, t);
//        }

//        public void Execute()
//        {
//            int2 pixel = ThreadIds.XY;
//            float2 uv = (float2)pixel / new float2(width, height);
//            float2 screenPos = uv * new float2(width, height);

//            float3 rayOrigin = worldData.cameraOrigin;
//            float3 rayDir = GetCameraRayDir(uv);

//            float4 finalColor = float4.Zero;

//            // Process all handles in reverse order (so later handles appear on top)
//            for (int i = handles.Length - 1; i >= 0; i--)
//            {
//                HandleDTO handle = handles[i];
//                float4 handleColor = float4.Zero;

//                switch (handle.type)
//                {
//                    case HandleType.Line:
//                        float3 start = handle.position;
//                        float3 end = handle.position + handle.parameters.XYZ;
//                        handleColor = DrawLine(start, end, handle.color, handle.size, uv, screenPos, rayOrigin, rayDir);
//                        break;

//                    case HandleType.Arrow:
//                        handleColor = DrawArrow(handle.position, handle.parameters.XYZ, handle.color, handle.size, uv, screenPos, rayOrigin, rayDir);
//                        break;

//                    case HandleType.Circle:
//                        handleColor = DrawCircle(handle.position, handle.size, handle.color, handle.size * 0.05f, uv, screenPos, rayOrigin, rayDir);
//                        break;

//                    case HandleType.Sphere:
//                        handleColor = DrawSphere(handle.position, handle.size, handle.color, uv, screenPos, rayOrigin, rayDir);
//                        break;

//                    case HandleType.Cube:
//                        handleColor = DrawCube(handle.position, handle.scale, handle.color, rayOrigin, rayDir);
//                        break;

//                    case HandleType.Icon:
//                        handleColor = DrawIcon(handle.position, handle.color, handle.size, handle.iconUV, uv, screenPos, rayOrigin, rayDir);
//                        break;

//                    case HandleType.TransformGizmo:
//                        handleColor = DrawTransformGizmo(handle.position, handle.rotation, handle.size, uv, screenPos, rayOrigin, rayDir);
//                        break;
//                }

//                // Blend handle with existing color (additive with alpha)
//                if (handleColor.A > 0f)
//                {
//                    finalColor += handleColor * handleColor.A;
//                }
//            }

//            // Blend with existing pixel color
//            float4 existingColor = colorTexture[pixel];
//            colorTexture[pixel] = new float4(
//                existingColor.RGB * (1f - finalColor.A) + finalColor.RGB,
//                Hlsl.Max(existingColor.A, finalColor.A)
//            );
//        }
//    }
//}
