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
    /// Represents a cone SDF.
    /// </summary>
    public class SDFCone : IComponent
    {
        /// <summary>
        /// Cone with angles of 0.6 and 0.4 and height of 3.0.
        /// </summary>
        public SDFCone()
        {
            cone = new float2(0.6f, 0.4f);
            height = 3f;
        }

        public float2 cone;
        public float height;

        public IComponent Clone() => new SDFCone
        {
            cone = cone,
            height = height,
        };
    }
}
