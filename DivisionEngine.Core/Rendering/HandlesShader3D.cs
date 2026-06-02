//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;

#pragma warning disable CA1416 // Validate platform compatibility

namespace DivisionEngine.Rendering
{
    /// <summary>
    /// Renders editor transformation handles as screen-space overlays on SDF-rendered scenes.
    /// Draws colored axis lines and a center circle without raymarching.
    /// </summary>
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct EditorHandleShader(
        float width,
        float height,
        float aspect,
        float camScreenDist,
        float3 cameraOrigin,
        float3 camForward,
        float3 camRight,
        float3 camUp,
        ReadWriteTexture2D<float4> renderTexture,
        ReadWriteBuffer<uint> handleIdBuffer,
        float3 handlePosition,
        float handleScale,
        uint hoveredHandle) : IComputeShader
    {
        /// <summary>
        /// Projects a 3D world position to 2D screen pixel coordinates.
        /// </summary>
        private float3 WorldToScreen(float3 worldPos)
        {
            float3 relativePos = worldPos - cameraOrigin;

            float camX = Hlsl.Dot(relativePos, camRight);
            float camY = Hlsl.Dot(relativePos, camUp);
            float camZ = Hlsl.Dot(relativePos, camForward);

            if (camZ <= 0.001f) return new float3(-1, -1, -1);

            float2 uv = new float2(camX / camZ / camScreenDist, camY / camZ / camScreenDist);
            uv.X = uv.X / aspect;

            float screenX = (uv.X * 0.5f + 0.5f) * width;
            float screenY = (uv.Y * 0.5f + 0.5f) * height;
            return new float3(screenX, screenY, camZ);
        }

        /// <summary>
        /// Returns the signed distance to a line segment.
        /// </summary>
        private float LineSegmentSDF(float2 p, float2 a, float2 b)
        {
            float2 pa = p - a;
            float2 ba = b - a;
            float h = Hlsl.Saturate(Hlsl.Dot(pa, ba) / Hlsl.Dot(ba, ba));
            return Hlsl.Length(pa - ba * h);
        }

        /// <summary>
        /// Returns the signed distance to a circle.
        /// </summary>
        private float CircleOutlineSDF(float2 p, float2 center, float radius, float thickness)
        {
            float d = Hlsl.Length(p - center) - radius;
            if (d < 0) return float.MaxValue;
            return d - thickness;
        }

        /// <summary>
        /// Returns the signed distance to a square (axis-aligned rectangle).
        /// </summary>
        private float SquareSDF(float2 p, float2 center, float size, float thickness)
        {
            float2 halfSize = new float2(size * 0.5f, size * 0.5f);
            float2 d = Hlsl.Abs(p - center) - halfSize;
            float distToSquare = Hlsl.Max(d.X, d.Y);
            if (distToSquare < 0) return float.MaxValue;
            return distToSquare - thickness;
        }

        /// <summary>
        /// Returns the final color of a handle SDF.
        /// </summary>
        private float3 DrawSDF(float distance, float thickness, float3 color, float3 currentColor, bool isHovered)
        {
            float alpha = Hlsl.Saturate(1.0f - Hlsl.Abs(distance) / thickness);
            if (isHovered)
            {
                alpha = Hlsl.Saturate(alpha * 1.2f);
                color *= 1.5f;
            }
            return Hlsl.Lerp(currentColor, color, alpha);
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            float2 uv = new float2(pixel.X, pixel.Y);

            float axisLength = 1.0f * handleScale;
            float scaleHandlePosition = axisLength * 0.6f; // Position of scale square (60% along the axis)

            // Calculate end points in world space
            float3 xEnd = handlePosition + new float3(axisLength, 0, 0);
            float3 yEnd = handlePosition + new float3(0, axisLength, 0);
            float3 zEnd = handlePosition + new float3(0, 0, axisLength);

            // Scale handle positions in world space
            float3 xScale = handlePosition + new float3(scaleHandlePosition, 0, 0);
            float3 yScale = handlePosition + new float3(0, scaleHandlePosition, 0);
            float3 zScale = handlePosition + new float3(0, 0, scaleHandlePosition);

            // Project main points to screen
            float3 centerScreen = WorldToScreen(handlePosition);
            float3 xEndScreen = WorldToScreen(xEnd);
            float3 yEndScreen = WorldToScreen(yEnd);
            float3 zEndScreen = WorldToScreen(zEnd);

            // Project scale handle positions to screen
            float3 xScaleScreen = WorldToScreen(xScale);
            float3 yScaleScreen = WorldToScreen(yScale);
            float3 zScaleScreen = WorldToScreen(zScale);

            if (centerScreen.Z < 0) return;

            // Calculate screen-space arrow directions
            float2 xDir = Hlsl.Normalize(xEndScreen.XY - centerScreen.XY);
            float2 yDir = Hlsl.Normalize(yEndScreen.XY - centerScreen.XY);
            float2 zDir = Hlsl.Normalize(zEndScreen.XY - centerScreen.XY);

            // Perpendicular directions for arrow wings
            float2 xPerp = new float2(-xDir.Y, xDir.X);
            float2 yPerp = new float2(-yDir.Y, yDir.X);
            float2 zPerp = new float2(-zDir.Y, zDir.X);

            // Arrow wing endpoints
            float arrowSize = 15.0f;
            float2 xArrow1 = xEndScreen.XY - xDir * arrowSize * 0.8f + xPerp * arrowSize * 0.5f;
            float2 xArrow2 = xEndScreen.XY - xDir * arrowSize * 0.8f - xPerp * arrowSize * 0.5f;
            float2 yArrow1 = yEndScreen.XY - yDir * arrowSize * 0.8f + yPerp * arrowSize * 0.5f;
            float2 yArrow2 = yEndScreen.XY - yDir * arrowSize * 0.8f - yPerp * arrowSize * 0.5f;
            float2 zArrow1 = zEndScreen.XY - zDir * arrowSize * 0.8f + zPerp * arrowSize * 0.5f;
            float2 zArrow2 = zEndScreen.XY - zDir * arrowSize * 0.8f - zPerp * arrowSize * 0.5f;

            float lineThickness = 2f;
            float hoveredLineThickness = 4.0f;
            float circleRadius = 4.0f;
            float circleThickness = 1.5f;
            float squareSize = 5.0f; // Screen-space pixel size for scale squares

            // Use thicker lines when hovered
            float xThickness = (hoveredHandle == 1) ? hoveredLineThickness : lineThickness;
            float yThickness = (hoveredHandle == 2) ? hoveredLineThickness : lineThickness;
            float zThickness = (hoveredHandle == 3) ? hoveredLineThickness : lineThickness;
            float xScaleThickness = (hoveredHandle == 5) ? hoveredLineThickness : lineThickness;
            float yScaleThickness = (hoveredHandle == 6) ? hoveredLineThickness : lineThickness;
            float zScaleThickness = (hoveredHandle == 7) ? hoveredLineThickness : lineThickness;
            float circleThicknessActual = (hoveredHandle == 4) ? hoveredLineThickness : circleThickness;

            // Main axis lines
            float distX = (xEndScreen.Z > 0) ? LineSegmentSDF(uv, centerScreen.XY, xEndScreen.XY) : float.MaxValue;
            float distY = (yEndScreen.Z > 0) ? LineSegmentSDF(uv, centerScreen.XY, yEndScreen.XY) : float.MaxValue;
            float distZ = (zEndScreen.Z > 0) ? LineSegmentSDF(uv, centerScreen.XY, zEndScreen.XY) : float.MaxValue;

            // Arrow lines
            float distXArrow1 = (xEndScreen.Z > 0) ? LineSegmentSDF(uv, xEndScreen.XY, xArrow1) : float.MaxValue;
            float distXArrow2 = (xEndScreen.Z > 0) ? LineSegmentSDF(uv, xEndScreen.XY, xArrow2) : float.MaxValue;
            float distYArrow1 = (yEndScreen.Z > 0) ? LineSegmentSDF(uv, yEndScreen.XY, yArrow1) : float.MaxValue;
            float distYArrow2 = (yEndScreen.Z > 0) ? LineSegmentSDF(uv, yEndScreen.XY, yArrow2) : float.MaxValue;
            float distZArrow1 = (zEndScreen.Z > 0) ? LineSegmentSDF(uv, zEndScreen.XY, zArrow1) : float.MaxValue;
            float distZArrow2 = (zEndScreen.Z > 0) ? LineSegmentSDF(uv, zEndScreen.XY, zArrow2) : float.MaxValue;

            // Scale squares
            float distXScale = (xScaleScreen.Z > 0) ? SquareSDF(uv, xScaleScreen.XY, squareSize, xScaleThickness) : float.MaxValue;
            float distYScale = (yScaleScreen.Z > 0) ? SquareSDF(uv, yScaleScreen.XY, squareSize, yScaleThickness) : float.MaxValue;
            float distZScale = (zScaleScreen.Z > 0) ? SquareSDF(uv, zScaleScreen.XY, squareSize, zScaleThickness) : float.MaxValue;

            float distCenterCircle = CircleOutlineSDF(uv, centerScreen.XY, circleRadius, circleThicknessActual);

            float4 existingColor = renderTexture[pixel];
            float3 finalColor = existingColor.XYZ;
            uint handleId = 0;

            // Check center circle first
            if (Hlsl.Abs(distCenterCircle) <= circleThicknessActual)
            {
                handleId = 4;
                bool isHovered = hoveredHandle == 4;
                finalColor = DrawSDF(distCenterCircle, circleThicknessActual, new float3(1.0f, 1.0f, 1.0f), finalColor, isHovered);
            }
            // X Scale Square
            else if (Hlsl.Abs(distXScale) <= xScaleThickness)
            {
                handleId = 5;
                bool isHovered = hoveredHandle == 5;
                finalColor = DrawSDF(distXScale, xScaleThickness, new float3(1.0f, 0.2f, 0.2f), finalColor, isHovered);
            }
            // Y Scale Square
            else if (Hlsl.Abs(distYScale) <= yScaleThickness)
            {
                handleId = 6;
                bool isHovered = hoveredHandle == 6;
                finalColor = DrawSDF(distYScale, yScaleThickness, new float3(0.2f, 1.0f, 0.2f), finalColor, isHovered);
            }
            // Z Scale Square
            else if (Hlsl.Abs(distZScale) <= zScaleThickness)
            {
                handleId = 7;
                bool isHovered = hoveredHandle == 7;
                finalColor = DrawSDF(distZScale, zScaleThickness, new float3(0.2f, 0.6f, 1.0f), finalColor, isHovered);
            }
            // X axis
            else if (distX <= xThickness || distXArrow1 <= xThickness || distXArrow2 <= xThickness)
            {
                handleId = 1;
                bool isHovered = hoveredHandle == 1;
                if (distX <= xThickness)
                    finalColor = DrawSDF(distX, xThickness, new float3(1.0f, 0.2f, 0.2f), finalColor, isHovered);
                if (distXArrow1 <= xThickness)
                    finalColor = DrawSDF(distXArrow1, xThickness, new float3(1.0f, 0.2f, 0.2f), finalColor, isHovered);
                if (distXArrow2 <= xThickness)
                    finalColor = DrawSDF(distXArrow2, xThickness, new float3(1.0f, 0.2f, 0.2f), finalColor, isHovered);
            }
            // Y axis
            else if (distY <= yThickness || distYArrow1 <= yThickness || distYArrow2 <= yThickness)
            {
                handleId = 2;
                bool isHovered = hoveredHandle == 2;
                if (distY <= yThickness)
                    finalColor = DrawSDF(distY, yThickness, new float3(0.2f, 1.0f, 0.2f), finalColor, isHovered);
                if (distYArrow1 <= yThickness)
                    finalColor = DrawSDF(distYArrow1, yThickness, new float3(0.2f, 1.0f, 0.2f), finalColor, isHovered);
                if (distYArrow2 <= yThickness)
                    finalColor = DrawSDF(distYArrow2, yThickness, new float3(0.2f, 1.0f, 0.2f), finalColor, isHovered);
            }
            // Z axis
            else if (distZ <= zThickness || distZArrow1 <= zThickness || distZArrow2 <= zThickness)
            {
                handleId = 3;
                bool isHovered = hoveredHandle == 3;
                if (distZ <= zThickness)
                    finalColor = DrawSDF(distZ, zThickness, new float3(0.2f, 0.4f, 1.0f), finalColor, isHovered);
                if (distZArrow1 <= zThickness)
                    finalColor = DrawSDF(distZArrow1, zThickness, new float3(0.2f, 0.4f, 1.0f), finalColor, isHovered);
                if (distZArrow2 <= zThickness)
                    finalColor = DrawSDF(distZArrow2, zThickness, new float3(0.2f, 0.4f, 1.0f), finalColor, isHovered);
            }

            handleIdBuffer[pixel.X + pixel.Y * (int)width] = handleId;
            renderTexture[pixel] = new float4(finalColor, existingColor.W);
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
