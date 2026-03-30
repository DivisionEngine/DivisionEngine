//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Input;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Used to handle editor controls and shortcuts.
    /// </summary>
    internal class EditorControlsSystem : SystemBase
    {
        public override void Update()
        {
            if (InputSystem.IsPressed(KeyCode.Delete))
            {
                
            }
        }
    }
}
