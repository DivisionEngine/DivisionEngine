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
using DivisionEngine.Input;
using static DivisionEngine.Debug;

namespace DivisionEngine.Tests
{
    /// <summary>
    /// Used for testing the input system
    /// </summary>
    internal class TestInputSystem : SystemBase
    {
        public override void Update()
        {
            //Info($"K key is pressed: {InputSystem.IsPressed(KeyCode.K)}");
            //Info($"Left mouse is pressed: {InputSystem.IsMousePressed(MouseCode.Left)}");
            //Info($"Mouse position: ({InputSystem.MouseUV.X}, {InputSystem.MouseUV.Y})");
        }
    }
}
