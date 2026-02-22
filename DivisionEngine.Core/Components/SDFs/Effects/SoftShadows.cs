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
namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to cast and receive shadows.
    /// </summary>
    public class SoftShadows : IComponent
    {
        /// <summary>
        /// Shadow casters and recievers both enabled with a max distance of 100.0.
        /// </summary>
        public SoftShadows()
        {
            shadowCaster = true;
            shadowReceiver = true;

            minDistance = 0.001f;
            maxDistance = 100f;
        }

        public bool shadowCaster;
        public bool shadowReceiver;

        public float minDistance;
        public float maxDistance;

        public IComponent Clone() => new SoftShadows
        {
            shadowCaster = shadowCaster,
            shadowReceiver = shadowReceiver,

            minDistance = minDistance,
            maxDistance = maxDistance,
        };
    }
}
