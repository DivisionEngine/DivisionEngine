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
    public readonly partial struct CustomShapeShader(
        float width,
        float height,
        float aspect,
        float camScreenDist,
        float3 cameraOrigin,
        float3 camForward,
        float3 camRight,
        float3 camUp,
        ReadWriteTexture2D<float4> renderTexture,
        ReadWriteBuffer<uint> shapeIdBuffer,
        HandleShape shape,
        uint shapeId) : IComputeShader
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

        private float LineSDF(float2 p, float2 a, float2 b, float thickness)
        {
            float2 pa = p - a;
            float2 ba = b - a;
            float h = Hlsl.Saturate(Hlsl.Dot(pa, ba) / Hlsl.Dot(ba, ba));
            float d = Hlsl.Length(pa - ba * h);
            return d - thickness;
        }

        private float CircleSDF(float2 p, float2 center, float radius, float thickness)
        {
            float d = Hlsl.Length(p - center) - radius;
            if (d < 0) return float.MaxValue;
            return d - thickness;
        }

        private float3 DrawShape(float distance, float thickness, float3 color, float3 currentColor)
        {
            if (distance > thickness) return currentColor;
            float alpha = Hlsl.Saturate(1.0f - distance / thickness);
            return Hlsl.Lerp(currentColor, color, alpha);
        }

        public void Execute()
        {
            int2 pixel = ThreadIds.XY;
            float2 uv = new float2(pixel.X, pixel.Y);

            float4 existingColor = renderTexture[pixel];
            float3 finalColor = existingColor.XYZ;
            float distance = float.MaxValue;

            if (shape.Type == 0)
            {
                float3 startScreen = WorldToScreen(shape.Start);
                float3 endScreen = WorldToScreen(shape.End);
                if (startScreen.Z > 0 && endScreen.Z > 0)
                {
                    distance = LineSDF(uv, startScreen.XY, endScreen.XY, shape.Thickness);
                }
            }
            else if (shape.Type == 1 || shape.Type == 2 || shape.Type == 3)
            {
                float3 centerScreen = WorldToScreen(shape.Center);
                if (centerScreen.Z > 0)
                {
                    // For spheres, we need to handle the projection properly
                    // For now, just draw a circle
                    float screenRadius = shape.Radius / centerScreen.Z * camScreenDist;
                    distance = CircleSDF(uv, centerScreen.XY, screenRadius * 50, shape.Thickness);
                }
            }

            if (distance <= shape.Thickness)
            {
                shapeIdBuffer[pixel.X + pixel.Y * (int)width] = shapeId;
                finalColor = DrawShape(distance, shape.Thickness, shape.Color, finalColor);
                renderTexture[pixel] = new float4(finalColor, 1.0f);
            }
        }
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
