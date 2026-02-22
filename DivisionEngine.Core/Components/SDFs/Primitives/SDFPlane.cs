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
namespace DivisionEngine.Components.SDFs.Primitives
{
    /// <summary>
    /// Represents a plane SDF.
    /// </summary>
    public class SDFPlane : IComponent
    {
        /// <summary>
        /// Plane with a normal vector directly up and a height of 1.0.
        /// </summary>
        public SDFPlane()
        {
            normal = new float3(0f, 1f, 0f);
            height = 1f;
        }

        public float3 normal;
        public float height;

        public IComponent Clone() => new SDFPlane
        {
            normal = normal,
            height = height,
        };
    }
}
