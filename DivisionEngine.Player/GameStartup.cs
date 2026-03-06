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
using DivisionEngine.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace DivisionEngine.Player;

/// <summary>
/// Represents the main entry point for the game application.
/// </summary>
public class GameStartup
{
    public static RenderPipeline? Renderer { get; private set; }
    public static InputSystem? UserInput { get; private set; }

    public const int EngineFrameTimeMS = 16; // Around 60 fps
    public const double RequestedFPS = 60;

    private static Task? engineCoreTask;
    private static CancellationTokenSource? engineCancellationTokenSource;

    /// <summary>
    /// The main entry point for the game.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        UserInput = new InputSystem();

        // Replace with project path from startup args eventually.
        LoadProjectOrDefaultWorld(string.Empty);

        // Run engine loop
        engineCancellationTokenSource = new CancellationTokenSource();
        engineCoreTask = Task.Run(() => RunEngineLoop(EngineFrameTimeMS / 2, engineCancellationTokenSource.Token));

        // Run render pipeline
        Renderer = new RenderPipeline();
        Renderer.InputContextCreated += SetupInputHandlers; // Subscribe input handling at correct time!
        Renderer.Close += EngineCore.Stop; // Stop engine loop

        Renderer.BindCurrentWorld(); // Bind loaded project
        Renderer.Run(RequestedFPS, false);

        // Cancel and stop engine loop
        engineCancellationTokenSource.Cancel();
        engineCoreTask?.Wait(1000);
        engineCancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Runs the main engine core loop.
    /// </summary>
    /// <param name="frameTime">Frame time the main loop runs at (usually 16ms for 60fps)</param>
    /// <param name="cancellationToken">Engine loop thread cancellation token</param>
    private static void RunEngineLoop(int frameTime, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                EngineCore.RunFrame();
                Thread.Sleep(frameTime);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Loads the project path provided or a default world if the path is empty into the current world.
    /// </summary>
    /// <param name="projectPath">Path to project</param>
    /// <returns>The world loaded into the current world</returns>
    private static World LoadProjectOrDefaultWorld(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            return WorldManager.CreateDefaultWorld(true);
        }
        else
        {
            // Implement project loading here, for now fallback to default world
            return WorldManager.CreateDefaultWorld(true);
        }
    }

    /// <summary>
    /// Setup input handling for borderless Silk.NET threaded render GLFW window.
    /// </summary>
    private static void SetupInputHandlers(IInputContext input)
    {
        Debug.Info("Setup Input Handlers: Configuring input...");
        foreach (IKeyboard keyboard in input.Keyboards)
        {
            keyboard.KeyDown += (kb, key, code) => UserInput?.SetKeyDown(PlayerInput.SilkNetToKeyCode(key));
            keyboard.KeyUp += (kb, key, code) => UserInput?.SetKeyUp(PlayerInput.SilkNetToKeyCode(key));
        }

        foreach (IMouse mouse in input.Mice)
        {
            mouse.MouseDown += (m, code) => UserInput?.SetMouseKeyDown(PlayerInput.SilkNetToMouseCode(code));
            mouse.MouseUp += (m, code) => UserInput?.SetMouseKeyUp(PlayerInput.SilkNetToMouseCode(code));

            mouse.MouseMove += (m, pos) =>
            {
                float2 posConverted = new float2(pos.X, pos.Y);
                UserInput?.SetMousePosition(posConverted);

                if (Renderer == null || Renderer.RendererWindow == null) return;
                Vector2D<int> screenSizeInt = Renderer.RendererWindow.Size;
                float2 screenSize = new float2(screenSizeInt.X, screenSizeInt.Y);
                UserInput?.SetRelativeMousePosition(posConverted, screenSize);
            };

            mouse.Scroll += (m, wheel) =>
            {
                float2 posConverted = new float2(wheel.X, wheel.Y);
                UserInput?.SetMouseWheel(posConverted);
            };
        }
        Debug.Info("Setup Input Handlers: Input configured successfully");
    }
}