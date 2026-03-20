//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
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
