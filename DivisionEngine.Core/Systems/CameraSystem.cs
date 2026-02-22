//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using ComputeSharp;
using DivisionEngine.Components;
using DivisionEngine.MathLib;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// In charge of processing render information for all cameras in the world.
    /// </summary>
    public class CameraSystem : SystemBase
    {
        /*private static void UpdateCameraMatrices(Transform transform, Camera camera)
        {
            float4x4 camToWorld = CalcCameraToWorldMatrix(transform);
            camera.cameraToWorld = camToWorld;
            camera.viewMatrix = Matrix.Inverse(camToWorld);
            camera.projectionMatrix = CalcCameraProjectionMatrix(camera);
            camera.inverseProjectionMatrix = Matrix.Inverse(camera.projectionMatrix);
        }

        private static float4x4 CalcCameraToWorldMatrix(Transform t)
        {
            float3 forward = t.Forward;
            float3 right = t.Right;
            float3 up = t.Up;

            //Debug.Info($"F {forward}");
            //Debug.Info($"R {right}");
            Debug.Info($"U {up}");

            return new float4x4(
                right.X, right.Y, right.Z, 0,
                up.X, up.Y, up.Z, 0,
                -forward.X, -forward.Y, -forward.Z, 0,
                t.position.X, t.position.Y, t.position.Z, 1
            );
        }

        private static float4x4 CalcCameraProjectionMatrix(Camera cam)
        {
            float fovRad = Math.Deg2Rad * cam.fieldOfView;
            float tanHalfFov = Math.Tan(fovRad / 2f);

            float m1122 = 1f / tanHalfFov; // (usually 1f / (aspect * tanHalfFov)) but aspect ratio is in shader instead
            //float m22 = 1f / tanHalfFov;
            float m33 = cam.farClip / (cam.nearClip - cam.farClip);
            float m43 = (cam.farClip * cam.nearClip) / (cam.nearClip - cam.farClip);

            return new float4x4(
                m1122, 0, 0, 0,
                0, m1122, 0, 0,
                0, 0, m33, -1,
                0, 0, m43, 0);
        }*/

        public static float FovToScreenDistance(Camera cam)
        {
            float fovRadians = cam.fieldOfView * Math.PI / 180f;
            return Math.Tan(fovRadians * 0.5f);
        }
    }
}
