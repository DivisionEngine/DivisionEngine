//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DivisionEngine.Editor.Settings;
using DivisionEngine.Editor.ViewModels;
using DivisionEngine.Input;
using DivisionEngine.Projects;
using DivisionEngine.Rendering;
using DivisionEngine.Settings;
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
        /// Whether the application is focused or not.
        /// </summary>
        public static event Action<bool>? AppFocused;

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
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

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
                Renderer = new RenderPipeline();
                Renderer.BindCurrentWorld();

                // Subscribe to input context creation before starting the renderer
                Renderer.InputContextCreated += SetupInputHandlers;

                _ = Task.Run(() => Renderer.Run(RequestedFPS, true));
                Renderer.Close += () =>
                {
                    EngineCore.Stop(); // Stop engine loop
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                            desktop.Shutdown();
                        Environment.Exit(0);
                    });
                };

                // Wait for renderer to be ready
                // while (Renderer == null || !Renderer.InputReady) await Task.Delay(1);
                EnvironmentWindow.SyncToolValuesToRenderer(); // Update environment window tools
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
                // Settings are automatically loaded when Instance is first accessed
                EditorSettings settings = EditorSettings.Instance;

                desktop.MainWindow = new MainWindow();
                MainWindowViewModel vm = new MainWindowViewModel(desktop.MainWindow);
                desktop.MainWindow.DataContext = vm;

                // Startup editor
                WorldManager.CreateDefaultWorld(true);
                StartEditorEngineLoop();
                UserInput = new InputSystem();
                _ = SetEditorRenderingAsync(true); // Start the SDFRenderer in a separate thread
                SetupAvaloniaInput(desktop);

                // Bind editor callbacks
                desktop.Exit += (s, e) =>
                {
                    Debug.Info($"Editor application exit with code: {e.ApplicationExitCode}");
                    SettingsManager.SaveSettings(settings); // Save editor settings on editor close
                    Renderer?.RendererWindow?.Close();

                    // Close project if open on exit
                    if (ProjectManager.IsCurrentLoaded) ProjectManager.CloseProject();
                };
                desktop.MainWindow.Activated += (_, _) => AppFocused!(true);
                desktop.MainWindow.Deactivated += (_, _) => AppFocused!(false);
                vm.RequestClose += () => desktop.Shutdown(); // Bind close request
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Runs the ECS main loop in the editor, referencing EngineCore.
        /// </summary>
        private void StartEditorEngineLoop()
        {
            // Create Avalonia editor integrated engine loop
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
        private static async void SetupAvaloniaInput(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avalonia input handling
            desktop.MainWindow!.KeyUp += (s, e) => UserInput?.SetKeyUp(EditorInput.AvaloniaToKeyCode(e.Key));
            desktop.MainWindow.KeyDown += (s, e) => UserInput?.SetKeyDown(EditorInput.AvaloniaToKeyCode(e.Key));
        }

        /// <summary>
        /// Setup input handling for borderless Silk.NET threaded render GLFW window.
        /// </summary>
        private static void SetupInputHandlers(IInputContext input)
        {
            Debug.Info("Setup Input Handlers: Configuring input...");
            foreach (IKeyboard keyboard in input.Keyboards)
            {
                keyboard.KeyDown += (kb, key, code) => UserInput?.SetKeyDown(EditorInput.SilkNetToKeyCode(key));
                keyboard.KeyUp += (kb, key, code) => UserInput?.SetKeyUp(EditorInput.SilkNetToKeyCode(key));
            }

            foreach (IMouse mouse in input.Mice)
            {
                mouse.MouseDown += (m, code) => UserInput?.SetMouseKeyDown(EditorInput.SilkNetToMouseCode(code));
                mouse.MouseUp += (m, code) => UserInput?.SetMouseKeyUp(EditorInput.SilkNetToMouseCode(code));

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
}