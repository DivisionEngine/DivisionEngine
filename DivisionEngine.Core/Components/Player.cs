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
namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents an entity with player controls (WASD, LShift, Mouse Look).
    /// </summary>
    public class Player : IComponent
    {
        /// <summary>
        /// Default player controls (speed = 4, mouse sensitivity = 2, sprint multiplier = 2).
        /// </summary>
        public Player()
        {
            movementSpeed = 4f;
            mouseSensitivity = 2f;
            sprintMultiplier = 2f;
        }

        public float movementSpeed;
        public float mouseSensitivity;
        public float sprintMultiplier;

        public IComponent Clone() => new Player
        {
            movementSpeed = movementSpeed,
            mouseSensitivity = mouseSensitivity,
            sprintMultiplier = sprintMultiplier,
        };
    }
}
