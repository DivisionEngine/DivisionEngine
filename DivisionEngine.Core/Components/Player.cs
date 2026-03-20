//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
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
