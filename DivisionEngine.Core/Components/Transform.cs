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
using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Component for translation, rotation, and scaling.
    /// </summary>
    public class Transform : IComponent
    {
        /// <summary>
        /// Sets the position to (0, 0, 0), rotation to identity quaternion, and scaling to (1, 1, 1).
        /// </summary>
        public Transform()
        {
            position = new float3(0f, 0f, 0f);
            rotation = Quaternion.Identity;
            scaling = new float3(1f, 1f, 1f);
        }

        public float3 position;
        [Rotation(true)] public float4 rotation;
        public float3 scaling;

        public float3 Forward => rotation.RotateVector(new float3(0, 0, -1)).Normalize();
        public float3 Back => rotation.RotateVector(new float3(0, 0, 1)).Normalize();
        public float3 Up => rotation.RotateVector(new float3(0, 1, 0)).Normalize();
        public float3 Down => rotation.RotateVector(new float3(0, -1, 0)).Normalize();
        public float3 Left => rotation.RotateVector(new float3(-1, 0, 0)).Normalize();
        public float3 Right => rotation.RotateVector(new float3(1, 0, 0)).Normalize();

        /*private static float3x3 RotAxis(float3 axis, float angle)
        {
            float s = Math.Sin(angle);
            float c = Math.Cos(angle);
            float oc = 1f - c;
            return new float3x3(
                oc * axis.X * axis.X + c, oc * axis.X * axis.Y - axis.Z * s, oc * axis.Z * axis.X + axis.Y * s,
                oc * axis.X * axis.Y + axis.Z * s, oc * axis.Y * axis.Y + c, oc * axis.Y * axis.Z - axis.X * s,
                oc * axis.Z * axis.X - axis.Y * s, oc * axis.Y * axis.Z + axis.X * s, oc * axis.Z * axis.Z + c
            );
        }*/

        public IComponent Clone() => new Transform
        {
            position = position,
            rotation = rotation,
            scaling = scaling,
        };
    }
}
