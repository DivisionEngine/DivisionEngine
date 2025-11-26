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
        SetupInput();

        // Replace with project path from startup args eventually.
        LoadProjectOrDefaultWorld(string.Empty);

        // Run engine loop
        engineCancellationTokenSource = new CancellationTokenSource();
        engineCoreTask = Task.Run(() => RunEngineLoop(EngineFrameTimeMS, engineCancellationTokenSource.Token));

        // Run render pipeline
        Renderer = new RenderPipeline();
        Renderer.BindCurrentWorld(); // Bind loaded project
        Renderer.Run(false);

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
        EngineCore.Start();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                EngineCore.RunFrame();
                Thread.Sleep(frameTime);
            }
        }
        catch (OperationCanceledException)
        { }
        finally { EngineCore.Stop(); }
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
    /// Configures the input system for the player build.
    /// </summary>
    private static async void SetupInput()
    {
        // Silk.NET input handling
        while (Renderer == null || Renderer!.RendererWindow == null)
            await Task.Delay(1); // Wait for the renderer to load

        Renderer!.RendererWindow.Load += SilkNetInputSetup;
    }

    /// <summary>
    /// Setup input handling for Silk.Net threaded render GL window.
    /// </summary>
    private static void SilkNetInputSetup()
    {
        lock (Renderer!.SyncLock)
        {
            IInputContext? input = Renderer!.RendererWindow!.CreateInput();
            foreach (var keyboard in input.Keyboards) // Keyboard handling
            {
                keyboard.KeyDown += (kb, key, code) => UserInput!.SetKeyDown(PlayerInput.SilkNetToKeyCode(key));
                keyboard.KeyUp += (kb, key, code) => UserInput!.SetKeyUp(PlayerInput.SilkNetToKeyCode(key));
            }

            Vector2D<int> screenSizeInt = Renderer!.RendererWindow!.Size;
            float2 screenSize = new float2(screenSizeInt.X, screenSizeInt.Y);
            foreach (var mouse in input.Mice) // Mouse handling
            {
                mouse.MouseDown += (m, code) => UserInput!.SetMouseKeyDown(PlayerInput.SilkNetToMouseCode(code));
                mouse.MouseUp += (m, code) => UserInput!.SetMouseKeyUp(PlayerInput.SilkNetToMouseCode(code));

                mouse.MouseMove += (m, pos) =>
                {
                    float2 posConverted = new float2(pos.X, pos.Y);
                    UserInput!.SetMousePosition(posConverted);
                    UserInput!.SetRelativeMousePosition(posConverted, screenSize);
                };
            }
        }
    }
}