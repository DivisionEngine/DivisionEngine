using System.Diagnostics;
using static DivisionEngine.Debug;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Manages the timing and frame rate of the engine.
    /// </summary>
    public class TimeSystem : SystemBase
    {
        private Stopwatch? timeTracker;
        private double lastRecordedTime, timeBetweenFrames; // in seconds

        /// <summary>
        /// Current delta time in the world (time between frames) in seconds.
        /// </summary>
        public static double DeltaTime { get; private set; }

        /// <summary>
        /// Current delta time in the world (time between frames) in seconds, floating point.
        /// </summary>
        public static float DeltaTimeF => (float)DeltaTime;

        /// <summary>
        /// Current time in the world.
        /// </summary>
        public static double Time { get; private set; }

        /// <summary>
        /// Current time in the world, floating point.
        /// </summary>
        public static float TimeF => (float)Time;

        public static int FrameCount { get; private set; }

        public static float FPS { get; private set; }

        public override void Awake()
        {
            lastRecordedTime = 0;
            FrameCount = 0;
            Time = 0;
            timeBetweenFrames = 0;
            timeTracker = new Stopwatch();
            timeTracker.Start();
            Info("Time started in current world");
        }

        public override void Update()
        {
            double newTime = timeTracker!.Elapsed.TotalSeconds;
            DeltaTime = newTime - lastRecordedTime;

            FrameCount++;
            timeBetweenFrames += DeltaTimeF;
            FPS = FrameCount / (float)timeBetweenFrames;
            Info($"Current FPS: {FPS}");
            Info($"Current FrameTime: {DeltaTimeF}");

            lastRecordedTime = timeTracker!.Elapsed.TotalSeconds;
            Time = lastRecordedTime;
        }
    }
}
