using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DivisionEngine.Editor.ViewModels;
using DivisionEngine.Input;
using DivisionEngine.Rendering;
using Silk.NET.Input;
using Silk.NET.Maths;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// Divsion Engine editor application backend.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Reference to the Division SDF render pipeline.
        /// </summary>
        public static RenderPipeline? Renderer { get; private set; }

        /// <summary>
        /// Current input system for the Division editor.
        /// </summary>
        public static InputSystem? UserInput { get; private set; }

        /// <summary>
        /// Whether the engine is currently rendering to a target window.
        /// </summary>
        public static bool RendererVisible { get; private set; }

        /// <summary>
        /// Constant FPS ms Division maintains in the editor.
        /// </summary>
        public const long EngineCoreFrameTime = 16; // Around 60 fps

        /// <summary>
        /// Constant FPS rate Division maintains in the editor.
        /// </summary>
        public const double RequestedFPS = 60;

        /// <summary>
        /// Initializes the Avalonia UI base app.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Sets whether the editor renders using a render pipeline.
        /// </summary>
        /// <param name="rendering">Whether the editor is rendering</param>
        public static async Task SetEditorRenderingAsync(bool rendering)
        {
            if (rendering && !RendererVisible)
            {
                RendererVisible = true;

                if (Renderer != null && Renderer.RendererWindow != null) Renderer.Stop();
                // Start the SDFRenderer in a separate thread
                Renderer = new RenderPipeline();
                Renderer.BindCurrentWorld(); // Binds default world
                _ = Task.Run(() => Renderer.Run(RequestedFPS, true));

                Renderer.Close += () =>
                {
                    // Shutdown UI Thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                            desktop.Shutdown();
                        Environment.Exit(0);
                    });
                };
                EnvironmentWindow.SyncToolValuesToRenderer(); // Sync tool values

                // Silk.NET input handling
                while (Renderer == null || Renderer!.RendererWindow == null)
                    await Task.Delay(1); // Wait for the renderer to load
                Renderer.RendererWindow.Load += SilkNetInputSetup;
            }
            else
            {
                RendererVisible = false;
                if (Renderer != null)
                {
                    Renderer!.Stop();
                    Renderer = null;
                }
            }
        }

        /// <summary>
        /// Runs when Avalonia finishes initialization.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow();
                MainWindowViewModel vm = new MainWindowViewModel(desktop.MainWindow);
                desktop.MainWindow.DataContext = vm;

                // Create default world for editor
                WorldManager.CreateDefaultWorld(true);

                // Start the editor engine loop
                StartEditorEngineLoop();

                // Initialize the input system
                UserInput = new InputSystem();

                // Start the SDFRenderer in a separate thread
                _ = SetEditorRenderingAsync(true);

                // Close the renderer window when the application exits
                desktop.Exit += (_, _) =>
                {
                    Renderer?.RendererWindow?.Close();
                };

                // Close when menu item exit clicked
                vm.RequestClose += () => desktop.Shutdown();
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Runs the ECS main loop in the editor, referencing the EngineCore.
        /// </summary>
        private void StartEditorEngineLoop()
        {
            EngineCore.Start(); // Start engine

            // Create Avalonia editor-integrated engine loop
            DispatcherTimer engineTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(EngineCoreFrameTime / 2)
            };
            engineTimer.Tick += EngineTimer_Tick;
            engineTimer.Start();
        }

        /// <summary>
        /// Runs each frame of the core engine in the editor.
        /// </summary>
        /// <param name="sender">Sender obj</param>
        /// <param name="e">Event args</param>
        private void EngineTimer_Tick(object? sender, EventArgs e) => EngineCore.RunFrame();

        /// <summary>
        /// Sets up input handling for the Division Engine editor.
        /// </summary>
        /// <param name="desktop">The desktop application lifetime for Avalonia UI.</param>
        private static async void SetupInput(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avalonia input handling
            desktop.MainWindow!.KeyUp += (s, e) => UserInput?.SetKeyUp(EditorInput.AvaloniaToKeyCode(e.Key));
            desktop.MainWindow.KeyDown += (s, e) => UserInput?.SetKeyDown(EditorInput.AvaloniaToKeyCode(e.Key));
        }

        /// <summary>
        /// Setup input handling for borderless Silk.Net threaded render GL window.
        /// </summary>
        public static void SilkNetInputSetup()
        {
            lock (Renderer!.SyncLock)
            {
                try
                {
                    IInputContext? input = Renderer!.RendererWindow!.CreateInput();
                    foreach (var keyboard in input.Keyboards) // Keyboard handling
                    {
                        keyboard.KeyDown += (kb, key, code) => UserInput!.SetKeyDown(EditorInput.SilkNetToKeyCode(key));
                        keyboard.KeyUp += (kb, key, code) => UserInput!.SetKeyUp(EditorInput.SilkNetToKeyCode(key));
                    }

                    foreach (var mouse in input.Mice) // Mouse handling
                    {
                        mouse.MouseDown += (m, code) => UserInput!.SetMouseKeyDown(EditorInput.SilkNetToMouseCode(code));
                        mouse.MouseUp += (m, code) => UserInput!.SetMouseKeyUp(EditorInput.SilkNetToMouseCode(code));

                        mouse.MouseMove += (m, pos) =>
                        {
                            float2 posConverted = new float2(pos.X, pos.Y);
                            UserInput!.SetMousePosition(posConverted);

                            Vector2D<int> screenSizeInt = Renderer!.RendererWindow!.Size;
                            float2 screenSize = new float2(screenSizeInt.X, screenSizeInt.Y);
                            UserInput!.SetRelativeMousePosition(posConverted, screenSize);
                        };
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Debug.Warning($"Renderer already has input: {ex.Message}");
                }
            }
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            DataAnnotationsValidationPlugin[]? dataValidationPluginsToRemove =
                [.. BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>()];

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
                BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}