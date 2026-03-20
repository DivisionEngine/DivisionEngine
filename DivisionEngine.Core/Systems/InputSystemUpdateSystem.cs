//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Input;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Updates the input system from the current ECS world on the fixed update loop.
    /// </summary>
    internal class InputSystemUpdateSystem : SystemBase
    {
        public override void FixedUpdate()
        {
            InputSystem.Instance!.OnFixedUpdate();
        }
    }
}
