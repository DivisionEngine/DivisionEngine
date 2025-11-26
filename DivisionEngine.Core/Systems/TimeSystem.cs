using System.Diagnostics;
using static DivisionEngine.Debug;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Manages the timing and frame rate of the engine.
    /// </summary>
    public class TimeSystem : SystemBase
    {
        public const int FPSFramesMeasured = 20;

        private Stopwatch? timeTracker;
        private double lastRecordedTime, timeBetweenFrames; // in seconds
        private int fpsFrameCounter;

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
            fpsFrameCounter = 0;
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

            fpsFrameCounter++;
            FrameCount++;
            timeBetweenFrames += DeltaTimeF;
            FPS = fpsFrameCounter / (float)timeBetweenFrames;
            //Info($"Current FrameTime: {DeltaTimeF}");

            if (fpsFrameCounter > FPSFramesMeasured)
            {
                Info($"Current FPS: {FPS}");
                timeBetweenFrames = 0;
                fpsFrameCounter = 0;
            }

            lastRecordedTime = timeTracker!.Elapsed.TotalSeconds;
            Time = lastRecordedTime;
        }
    }
}
