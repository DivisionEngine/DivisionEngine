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
            WorldManager.CurrentWorld?.CallAwake();
        }

        /// <summary>
        /// Stop the engine.
        /// </summary>
        public static void Stop()
        {
            IsRunning = false;
            IsPaused = false;
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
            WorldManager.CurrentWorld?.CallFixedUpdate();
            return true;
        }
    }
}
