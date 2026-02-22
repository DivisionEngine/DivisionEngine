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
using DivisionEngine.Rendering;
using static DivisionEngine.Debug;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Manages the timing and frame rate of the engine.
    /// </summary>
    public class TimeSystem : SystemBase
    {
        public const int FPSFramesMeasured = 20;

        private double timeBetweenFrames; // in seconds
        private int fpsFrameCounter;

        /// <summary>
        /// Current delta time in the world (time between frames) in seconds.
        /// </summary>
        public static double DeltaTime => RenderPipeline.DeltaTime;

        /// <summary>
        /// Current delta time in the world (time between frames) in seconds, floating point.
        /// </summary>
        public static float DeltaTimeF => (float)DeltaTime;

        /// <summary>
        /// Current time in the world.
        /// </summary>
        public static double Time => RenderPipeline.Time;

        /// <summary>
        /// Current time in the world, floating point.
        /// </summary>
        public static float TimeF => (float)Time;

        /// <summary>
        /// Number of frames elapsed since world began.
        /// </summary>
        public static int FrameCount { get; private set; }

        /// <summary>
        /// Frames per second, recorded every 20 frames.
        /// </summary>
        public static float FPS { get; private set; }

        public override void Awake()
        {
            fpsFrameCounter = 0;
            FrameCount = 0;
            timeBetweenFrames = 0;
            Info("Time measurement began in current world");
        }

        public override void Render()
        {
            FrameCount++;
            fpsFrameCounter++;
            timeBetweenFrames += DeltaTime;
            if (fpsFrameCounter > FPSFramesMeasured)
            {
                FPS = fpsFrameCounter / (float)timeBetweenFrames;
                timeBetweenFrames = 0;
                fpsFrameCounter = 0;
            }
        }
    }
}
