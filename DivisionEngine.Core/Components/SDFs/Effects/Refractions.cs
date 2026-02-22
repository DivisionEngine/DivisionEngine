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
    /// Allows SDF objects to receive refractions.
    /// </summary>
    public class Refractions : IComponent
    {
        public Refractions()
        {
            hasRefractions = true;
            absorptionColor = new float4(1f, 1f, 1f, 0.1f);
            maxRaySteps = 196;
            maxRecursionTraces = 4;
        }

        public bool hasRefractions;
        [Color(ShowAlpha = true)] public float4 absorptionColor;
        public int maxRaySteps;
        [Range(1, 16)] public int maxRecursionTraces;

        public IComponent Clone() => new Refractions
        {
            maxRaySteps = maxRaySteps,
            absorptionColor = absorptionColor,
            hasRefractions = hasRefractions,
            maxRecursionTraces = maxRecursionTraces,
        };
    }
}
