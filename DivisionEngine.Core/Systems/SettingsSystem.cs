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
using DivisionEngine.Settings;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Initializes engine settings.
    /// </summary>
    internal class SettingsSystem : SystemBase
    {
        private int saveTimer;
        private const int SAVE_INTERVAL = 900; // Save every 900 frames (at 60 fps about 15 seconds, 30fps, 30s)
        private bool hasChanges;

        public override void Awake()
        {
            // Ensure engine settings are loaded
            _ = EngineSettings.Instance;
        }

        public override void Update()
        {
            if (!hasChanges) return;

            saveTimer += 1;
            if (saveTimer >= SAVE_INTERVAL)
            {
                SettingsManager.SaveAll();
                saveTimer = 0;
                hasChanges = false;
            }
        }

        public override void Unload() => SettingsManager.SaveAll();

        /// <summary>
        /// Call this whenever a setting changes.
        /// </summary>
        public void MarkDirty() => hasChanges = true;
    }
}
