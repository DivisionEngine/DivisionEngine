//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Editor.ViewModels;
using DivisionEngine.Input;
using DivisionEngine.Projects;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Used to handle editor controls and shortcuts.
    /// </summary>
    internal class EditorControlsSystem : SystemBase
    {
        public override void Update()
        {
            if (InputSystem.IsPressed(KeyCode.Delete) && W.EntityExists(Selection.Entity))
            {
                MainWindowViewModel.vm!.RecentControlsText = $"Delete : Removed Entity {Selection.Entity}";
                W.DestroyEntity(Selection.Entity);
                Selection.Clear();
            }
            else if (InputSystem.IsPressed(KeyCode.S) && InputSystem.IsCtrlPressed())
            {
                ProjectManager.SaveCurrentProject();
                MainWindowViewModel.vm!.RecentControlsText = $"(Ctrl + S) : Saved Project";
            }
        }
    }
}
