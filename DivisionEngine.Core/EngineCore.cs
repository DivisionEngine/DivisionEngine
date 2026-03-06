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
namespace DivisionEngine
{
    /// <summary>
    /// Core class that responsible for scheduling calls in Division Engine.
    /// </summary>
    public static class EngineCore
    {
        /// <summary>
        /// Is the engine running?
        /// </summary>
        public static bool IsRunning { get; private set; }

        /// <summary>
        /// Is the engine paused?
        /// </summary>
        public static bool IsPaused { get; private set; }

        /// <summary>
        /// Start the engine.
        /// </summary>
        public static void Start()
        {
            IsRunning = true;
            IsPaused = false;
            Debug.Info("Engine Core: Starting");
            WorldManager.CurrentWorld?.CallAwake();
        }

        /// <summary>
        /// Stop the engine.
        /// </summary>
        public static void Stop()
        {
            IsRunning = false;
            IsPaused = false;
            Debug.Info("Engine Core: Stopping");
            WorldManager.CurrentWorld?.CallUnload();
        }

        /// <summary>
        /// Pause the engine.
        /// </summary>
        public static void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Resume the engine.
        /// </summary>
        public static void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Execute a frame in the engine.
        /// </summary>
        /// <returns>Whether the frame was successfully executed</returns>
        public static bool RunFrame()
        {
            if (!IsRunning || IsPaused) return false;

            WorldManager.CurrentWorld?.CallUpdate();
            WorldManager.CurrentWorld?.CallFixedUpdate(); // Fixed update loop runs after update loop
            return true;
        }
    }
}
