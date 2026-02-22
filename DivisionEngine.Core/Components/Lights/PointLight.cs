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
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.Lights
{
    /// <summary>
    /// Represents a point light in the world.
    /// </summary>
    public class PointLight : IComponent
    {
        public PointLight()
        {
            color = ColorPalette.White;
            intensity = 1f;
            radius = 10f;
        }

        public float4 color;
        public float intensity;
        public float radius;

        public IComponent Clone() => new PointLight
        {
            color = color,
            intensity = intensity,
            radius = radius,
        };
    }
}
