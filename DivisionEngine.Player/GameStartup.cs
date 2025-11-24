using DivisionEngine.Input;
using DivisionEngine.Rendering;
using Silk.NET.Input;

namespace DivisionEngine.Player;

/// <summary>
/// Represents the main entry point for the game application.
/// </summary>
public class GameStartup
{
    public static RenderPipeline? Renderer { get; private set; }
    public static InputSystem? UserInput { get; private set; }

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

        Renderer = new RenderPipeline();
        Renderer.BindCurrentWorld();
        Renderer.Run(false);
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

    private static void SilkNetInputSetup()
    {
        lock (Renderer!.SyncLock)
        {
            IInputContext? input = Renderer!.RendererWindow!.CreateInput();
            foreach (var keyboard in input.Keyboards)
            {
                keyboard.KeyDown += (kb, key, code) => UserInput!.SetKeyDown(PlayerInput.SilkNetToKeyCode(key));
                keyboard.KeyUp += (kb, key, code) => UserInput!.SetKeyUp(PlayerInput.SilkNetToKeyCode(key));
            }
        }
    }
}