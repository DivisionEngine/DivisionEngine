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
    /// Main editor application backend.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Reference to the Division SDF render pipeline.
        /// </summary>
        public static RenderPipeline? Renderer { get; private set; }
        public static InputSystem? UserInput { get; private set; }

        public const long EngineCoreFrameTime = 16; // Around 60 fps

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };

                // Create default world for editor
                WorldManager.CreateDefaultWorld(true);

                // Start the editor engine loop
                StartEditorEngineLoop();

                // Start the SDFRenderer in a separate thread
                Renderer = new RenderPipeline();
                Renderer.BindCurrentWorld(); // Binds default world
                Task.Run(() => Renderer.Run(true));

                // Handle renderer close behavior
                Renderer.Close += () =>
                {
                    // Shutdown UI Thread
                    Dispatcher.UIThread.Post(() =>
                    {
                        desktop.Shutdown();
                        Environment.Exit(0);
                    });
                };

                // Initialize the input system
                UserInput = new InputSystem();
                SetupInput(desktop);

                // Close the renderer window when the application exits
                desktop.Exit += (_, _) =>
                {
                    Renderer?.RendererWindow?.Close();
                };
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
                Interval = TimeSpan.FromMilliseconds(EngineCoreFrameTime)
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

            // Expand to include avalonia 

            // Silk.NET input handling
            while (Renderer == null || Renderer!.RendererWindow == null)
                await Task.Delay(1); // Wait for the renderer to load

            Renderer!.RendererWindow.Load += SilkNetInputSetup;
        }

        /// <summary>
        /// Setup input handling for borderless Silk.Net threaded render GL window.
        /// </summary>
        private static void SilkNetInputSetup()
        {
            lock (Renderer!.SyncLock)
            {
                IInputContext? input = Renderer!.RendererWindow!.CreateInput();
                foreach (var keyboard in input.Keyboards) // Keyboard handling
                {
                    keyboard.KeyDown += (kb, key, code) => UserInput!.SetKeyDown(EditorInput.SilkNetToKeyCode(key));
                    keyboard.KeyUp += (kb, key, code) => UserInput!.SetKeyUp(EditorInput.SilkNetToKeyCode(key));
                }

                Vector2D<int> screenSizeInt = Renderer!.RendererWindow!.Size;
                float2 screenSize = new float2(screenSizeInt.X, screenSizeInt.Y);
                foreach (var mouse in input.Mice) // Mouse handling
                {
                    mouse.MouseDown += (m, code) => UserInput!.SetMouseKeyDown(EditorInput.SilkNetToMouseCode(code));
                    mouse.MouseUp += (m, code) => UserInput!.SetMouseKeyUp(EditorInput.SilkNetToMouseCode(code));

                    mouse.MouseMove += (m, pos) =>
                    {
                        float2 posConverted = new float2(pos.X, pos.Y);
                        UserInput!.SetMousePosition(posConverted);
                        UserInput!.SetRelativeMousePosition(posConverted, screenSize);
                    };
                }
            }
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}