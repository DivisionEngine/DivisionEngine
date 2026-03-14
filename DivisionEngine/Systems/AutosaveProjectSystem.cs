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
using DivisionEngine.Editor.Settings;
using DivisionEngine.Projects;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Auto-saves projects if EditorSettings.AutoSave is enabled.
    /// </summary>
    internal class AutosaveProjectSystem : SystemBase
    {
        private int saveProjectTimer;

        public override void Awake()
        {
            // Default save timer
            // 60 fps = save every 2 seconds, 30 fps = save every 4 seconds, etc.
            saveProjectTimer = 120;
        }

        public override void Update()
        {
            if (EditorSettings.Instance.AutoSave && ProjectManager.IsCurrentLoaded)
            {
                saveProjectTimer--;
                if (saveProjectTimer < 1)
                {
                    ProjectManager.SaveCurrentProject();
                    saveProjectTimer = EditorSettings.Instance.AutoSaveInterval;
                }
            }
        }

        public override void Unload()
        {
            if (EditorSettings.Instance.AutoSave && ProjectManager.IsCurrentLoaded)
                ProjectManager.SaveCurrentProject();
        }
    }
}
