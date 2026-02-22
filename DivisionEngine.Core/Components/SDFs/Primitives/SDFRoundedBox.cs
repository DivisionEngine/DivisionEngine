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
    /// Represents a rounded box SDF.
    /// </summary>
    public class SDFRoundedBox : IComponent
    {
        /// <summary>
        /// Rounded box with a size of 1.0 and a bevel of 0.05.
        /// </summary>
        public SDFRoundedBox()
        {
            size = new float3(1f, 1f, 1f);
            bevel = 0.05f;
        }

        public float3 size;
        public float bevel;

        public IComponent Clone() => new SDFRoundedBox
        {
            size = size,
            bevel = bevel,
        };
    }
}
