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

namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to receive reflections.
    /// </summary>
    public class Reflections : IComponent
    {
        public Reflections()
        {
            hasReflections = true;
            reflectionShadows = true;
            rayStepsFalloff = 3f;
            maxBounces = 2;
        }

        public bool hasReflections;
        public bool reflectionShadows;
        [Range(1f, 10f)] public float rayStepsFalloff;
        [Range(1, 16)] public int maxBounces;

        public IComponent Clone() => new Reflections
        {
            hasReflections = hasReflections,
            rayStepsFalloff = rayStepsFalloff,
            maxBounces = maxBounces,
        };
    }
}
