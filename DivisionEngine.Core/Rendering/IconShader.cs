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
    [GeneratedComputeShaderDescriptor]
    [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
    public readonly partial struct IconShader(
        float width,
        float height,
        float aspect,
        float camScreenDist,
        float3 cameraOrigin,
        float3 camForward,
        float3 camRight,
        float3 camUp,
        ReadWriteTexture2D<float4> renderTexture,
        ReadWriteBuffer<uint> iconIdBuffer,
        float3 iconPosition,
        uint iconType,
        float3 lightDirection,  // New parameter for directional light direction
        uint entityId) : IComputeShader
    {
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

        private float CircleSDF(float2 p, float2 center, float radius, float thickness)
        {
            float d = Hlsl.Length(p - center) - radius;
            if (d < 0) return float.MaxValue;
            return d - thickness;
        }

        private float FilledCircleSDF(float2 p, float2 center, float radius)
        {
            float d = Hlsl.Length(p - center) - radius;
            return d;
        }

        private float LineSDF(float2 p, float2 a, float2 b, float thickness)
        {
            float2 pa = p - a;
            float2 ba = b - a;
            float h = Hlsl.Saturate(Hlsl.Dot(pa, ba) / Hlsl.Dot(ba, ba));
            float d = Hlsl.Length(pa - ba * h);
            return d - thickness;
        }

        private float SquareSDF(float2 p, float2 center, float size, float thickness)
        {
            float2 halfSize = new float2(size * 0.5f, size * 0.5f);
            float2 d = Hlsl.Abs(p - center) - halfSize;
            float distToSquare = Hlsl.Max(d.X, d.Y);
            if (distToSquare < 0) return float.MaxValue;
            return distToSquare - thickness;
        }

        private float3 DrawShape(float distance, float thickness, float3 color, float3 currentColor)
        {
            if (distance > thickness) return currentColor;
            float alpha = Hlsl.Saturate(1.0f - distance / thickness);
            return Hlsl.Lerp(currentColor, color, alpha);
        }

        private void DrawCamera(float2 centerScreen, float scale, float2 uv, ref float3 finalColor, ref bool isHit)
        {
            float2 center = centerScreen;

            // Camera body (rounded rectangle) - Warm cream color
            float bodyWidth = 20 * scale;
            float bodyHeight = 12 * scale;
            float2 halfSize = new float2(bodyWidth * 0.5f, bodyHeight * 0.5f);
            float cornerRadius = 2 * scale;

            // Rounded rectangle SDF
            float2 q = Hlsl.Abs(uv - center) - halfSize;
            float distBody = Hlsl.Length(Hlsl.Max(q, 0.0f)) + Hlsl.Min(Hlsl.Max(q.X, q.Y), 0.0f) - cornerRadius;
            if (distBody <= 2.0f)
            {
                isHit = true;
                finalColor = DrawShape(distBody, 2.0f, new float3(0.25f, 0.25f, 0.75f), finalColor);
            }

            // Lens (large circle) - Light blue
            float distLens = CircleSDF(uv, center, 6 * scale, 2.0f);
            if (distLens <= 2.0f)
            {
                isHit = true;
                finalColor = DrawShape(distLens, 2.0f, new float3(0.5f, 0.7f, 0.95f), finalColor);
            }

            // Inner lens - Cyan
            float distInnerLens = FilledCircleSDF(uv, center, 4 * scale);
            if (distInnerLens <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distInnerLens, 1.5f, new float3(0.3f, 0.6f, 0.9f), finalColor);
            }

            // Lens reflection (cartoony sparkle) - Top-left of lens
            float2 sparkleCenter = center + new float2(2 * scale, 2 * scale);
            float distSparkle = FilledCircleSDF(uv, sparkleCenter, scale);
            if (distSparkle <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distSparkle, 1.5f, new float3(1.0f, 1.0f, 0.9f), finalColor);
            }
        }

        private void DrawDirectionalLight(float2 centerScreen, float scale, float2 uv, float3 direction, ref float3 finalColor, ref bool isHit)
        {
            float2 center = centerScreen;

            // Center circle
            float distCircle = CircleSDF(uv, center, 6 * scale, 2);
            if (distCircle <= 2)
            {
                isHit = true;
                finalColor = DrawShape(distCircle, 2, new float3(1.0f, 0.8f, 0.2f), finalColor);
            }

            float3 cameraDist = Hlsl.Distance(cameraOrigin, iconPosition);

            // Project the light direction to screen space
            // The direction is in world space, we need to project it to screen space
            float3 dirEnd = iconPosition + direction * 0.2f * cameraDist; // 2 units in the direction
            float3 dirEndScreen = WorldToScreen(dirEnd);
            float2 dirStart = centerScreen;
            float2 dirEnd2D = dirEndScreen.XY;

            // Draw the main direction line
            float distMainLine = LineSDF(uv, dirStart, dirEnd2D, 1.0f);
            if (distMainLine <= 1.0f)
            {
                isHit = true;
                finalColor = DrawShape(distMainLine, 1.0f, new float3(1.0f, 0.9f, 0.3f), finalColor);
            }

            // Draw arrow head at the end of direction line
            float2 dirVec = Hlsl.Normalize(dirEnd2D - dirStart);
            float2 perp = new float2(-dirVec.Y, dirVec.X);
            float arrowSize = 8 * scale;

            float2 arrowLeft = dirEnd2D - dirVec * arrowSize * 0.5f + perp * arrowSize * 0.3f;
            float2 arrowRight = dirEnd2D - dirVec * arrowSize * 0.5f - perp * arrowSize * 0.3f;

            float distArrowLeft = LineSDF(uv, dirEnd2D, arrowLeft, 1.0f);
            float distArrowRight = LineSDF(uv, dirEnd2D, arrowRight, 1.0f);

            if (distArrowLeft <= 1.0f)
            {
                isHit = true;
                finalColor = DrawShape(distArrowLeft, 1.0f, new float3(1.0f, 0.9f, 0.3f), finalColor);
            }
            if (distArrowRight <= 1.0f)
            {
                isHit = true;
                finalColor = DrawShape(distArrowRight, 1.0f, new float3(1.0f, 0.9f, 0.3f), finalColor);
            }

            // Add 4 smaller rays radiating outward (for aesthetic)
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90.0f * 3.14159f / 180.0f;
                float2 rayDir = new float2(Hlsl.Cos(angle), Hlsl.Sin(angle));
                float2 rayStart = center + rayDir * 8 * scale;
                float2 rayEnd = center + rayDir * 14 * scale;

                float distRay = LineSDF(uv, rayStart, rayEnd, 1.2f);
                if (distRay <= 1.2f)
                {
                    isHit = true;
                    finalColor = DrawShape(distRay, 1.2f, new float3(1.0f, 0.7f, 0.1f), finalColor);
                }
            }
        }

        private void DrawPointLight(float2 centerScreen, float scale, float2 uv, ref float3 finalColor, ref bool isHit)
        {
            float2 center = centerScreen;

            // Inner circle
            float distInner = CircleSDF(uv, center, 6 * scale, 2);
            if (distInner <= 2)
            {
                isHit = true;
                finalColor = DrawShape(distInner, 2, new float3(1.0f, 0.8f, 0.2f), finalColor);
            }

            // Outer glow circle
            float distOuter = CircleSDF(uv, center, 10 * scale, 1);
            if (distOuter <= 1)
            {
                isHit = true;
                finalColor = DrawShape(distOuter, 1, new float3(1.0f, 0.6f, 0.1f), finalColor);
            }
        }

        private void DrawSpotLight(float2 centerScreen, float scale, float2 uv, ref float3 finalColor, ref bool isHit)
        {
            float2 center = centerScreen;

            // Circle base
            float distCircle = CircleSDF(uv, center, 6 * scale, 2);
            if (distCircle <= 2)
            {
                isHit = true;
                finalColor = DrawShape(distCircle, 2, new float3(1.0f, 0.8f, 0.2f), finalColor);
            }

            // Cone lines
            float2 start = center;
            float2 end1 = center + new float2(12 * scale, 8 * scale);
            float2 end2 = center + new float2(12 * scale, -8 * scale);

            float distLine1 = LineSDF(uv, start, end1, 1.5f);
            float distLine2 = LineSDF(uv, start, end2, 1.5f);

            if (distLine1 <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distLine1, 1.5f, new float3(1.0f, 0.8f, 0.2f), finalColor);
            }
            if (distLine2 <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distLine2, 1.5f, new float3(1.0f, 0.8f, 0.2f), finalColor);
            }
        }

        private void DrawEnvironment(float2 centerScreen, float scale, float2 uv, ref float3 finalColor, ref bool isHit)
        {
            float2 center = centerScreen;

            // Sun (small circle in corner) - Yellow
            float2 sunCenter = center + new float2(10 * scale, 6 * scale);
            float distSun = CircleSDF(uv, sunCenter, 3 * scale, 1.5f);
            if (distSun <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distSun, 1.5f, new float3(1.0f, 0.8f, 0.3f), finalColor);
            }

            // Small sun rays
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60.0f * 3.14159f / 180.0f;
                float2 rayDir = new float2(Hlsl.Cos(angle), Hlsl.Sin(angle));
                float2 rayStart = sunCenter + rayDir * 4 * scale;
                float2 rayEnd = sunCenter + rayDir * 7 * scale;

                float distRay = LineSDF(uv, rayStart, rayEnd, 1.0f);
                if (distRay <= 1.0f)
                {
                    isHit = true;
                    finalColor = DrawShape(distRay, 1.0f, new float3(1.0f, 0.7f, 0.2f), finalColor);
                }
            }

            // Background mountain (larger, lighter)
            float2 mountain1Peak = center + new float2(-6 * scale, -2 * scale);
            float2 mountain1Left = center + new float2(-16 * scale, -10 * scale);
            float2 mountain1Right = center + new float2(4 * scale, -10 * scale);

            // Left edge of mountain 1
            float distM1Left = LineSDF(uv, mountain1Left, mountain1Peak, 1.5f);
            if (distM1Left <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distM1Left, 1.5f, new float3(0.5f, 0.6f, 0.7f), finalColor);
            }

            // Right edge of mountain 1
            float distM1Right = LineSDF(uv, mountain1Peak, mountain1Right, 1.5f);
            if (distM1Right <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distM1Right, 1.5f, new float3(0.5f, 0.6f, 0.7f), finalColor);
            }

            // Mountain base line
            float distM1Base = LineSDF(uv, mountain1Left, mountain1Right, 1.5f);
            if (distM1Base <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distM1Base, 1.5f, new float3(0.5f, 0.6f, 0.7f), finalColor);
            }

            // Foreground mountain (smaller, darker)
            float2 mountain2Peak = center + new float2(8 * scale, -5 * scale);
            float2 mountain2Left = center + new float2(-2 * scale, -10 * scale);
            float2 mountain2Right = center + new float2(16 * scale, -10 * scale);

            // Left edge of mountain 2
            float distM2Left = LineSDF(uv, mountain2Left, mountain2Peak, 1.5f);
            if (distM2Left <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distM2Left, 1.5f, new float3(0.3f, 0.4f, 0.5f), finalColor);
            }

            // Right edge of mountain 2
            float distM2Right = LineSDF(uv, mountain2Peak, mountain2Right, 1.5f);
            if (distM2Right <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distM2Right, 1.5f, new float3(0.3f, 0.4f, 0.5f), finalColor);
            }

            // Snow cap on main mountain
            float2 snowLeft = mountain1Peak + new float2(-3 * scale, -1 * scale);
            float2 snowRight = mountain1Peak + new float2(3 * scale, -1 * scale);
            float distSnow = LineSDF(uv, snowLeft, snowRight, 1.5f);
            if (distSnow <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distSnow, 1.5f, new float3(0.9f, 0.9f, 1.0f), finalColor);
            }

            // Small snow triangles
            float2 snowTip = mountain1Peak + new float2(0, -2 * scale);
            float distSnowTip = LineSDF(uv, mountain1Peak, snowTip, 1.5f);
            if (distSnowTip <= 1.5f)
            {
                isHit = true;
                finalColor = DrawShape(distSnowTip, 1.5f, new float3(0.9f, 0.9f, 1.0f), finalColor);
            }
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            float2 uv = new float2(pixel.X, pixel.Y);

            float3 centerScreen = WorldToScreen(iconPosition);
            if (centerScreen.Z < 0) return;

            float scale = 1f; // Base scale for all icons
            float4 existingColor = renderTexture[pixel];
            float3 finalColor = existingColor.XYZ;
            bool isHit = false;

            switch (iconType)
            {
                case 100: // Camera
                    DrawCamera(centerScreen.XY, scale, uv, ref finalColor, ref isHit);
                    break;
                case 101: // DirectionalLight
                    DrawDirectionalLight(centerScreen.XY, scale, uv, lightDirection, ref finalColor, ref isHit);
                    break;
                case 102: // PointLight
                    DrawPointLight(centerScreen.XY, scale, uv, ref finalColor, ref isHit);
                    break;
                case 103: // SpotLight
                    DrawSpotLight(centerScreen.XY, scale, uv, ref finalColor, ref isHit);
                    break;
                case 104: // Environment
                    DrawEnvironment(centerScreen.XY, scale, uv, ref finalColor, ref isHit);
                    break;
            }

            if (isHit)
            {
                iconIdBuffer[pixel.X + pixel.Y * (int)width] = entityId;
                renderTexture[pixel] = new float4(finalColor, 1.0f);
            }
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
