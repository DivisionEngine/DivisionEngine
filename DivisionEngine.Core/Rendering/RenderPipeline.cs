//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using ComputeSharp;
using DivisionEngine.Components;
using DivisionEngine.Rendering.Denoising;
using DivisionEngine.Rendering.Effects;
using DivisionEngine.Rendering.Terrains;
using DivisionEngine.Settings;
using DivisionEngine.Systems;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Diagnostics.CodeAnalysis;
using Window = Silk.NET.Windowing.Window;

namespace DivisionEngine.Rendering
{

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    /// <summary>
    /// SDF Render pipeline for Division Engine.
    /// </summary>
    public class RenderPipeline
    {
        /// <summary>
        /// Represents the debug mode of the render pipeline, if any.
        /// </summary>
        public enum DebugMode
        {
            None = 0, Depth = 1, WorldNormals = 2, ObjectID = 3, RaySteps = 4, Shadows = 5, BRDF = 6, Specular = 7, Diffuse = 8,
        }

        // Special variables

        /// <summary>
        /// The current render pipeline instance running.
        /// </summary>
        public static RenderPipeline? Instance { get; private set; }

        /// <summary>
        /// Synchronization lock for thread safety
        /// </summary>
        public readonly Lock SyncLock = new Lock();
        private bool closeWindowWithCloseEvent = true;

        // OpenGL variables
        private GL? gl;
        private uint glTexture, glShaderProgram;
        private static bool deviceLost;
        private float currentDpiScale = 1f;

        // Rendering variables

        /// <summary>
        /// Reference to the bound world that the renderer is using.
        /// </summary>
        public World? boundWorld;

        /// <summary>
        /// Reference to the renderer window handle.
        /// </summary>
        public IWindow? RendererWindow { get; private set; }

        /// <summary>
        /// Reference to the renderer's graphics device handle.
        /// </summary>
        public GraphicsDevice? Device { get; private set; }

        /// <summary>
        /// Whether the renderer is ready to process input or not.
        /// </summary>
        public bool InputReady { get; private set; } = false;

        /// <summary>
        /// Event to handle window close actions.
        /// </summary>
        public event Action? Close;

        /// <summary>
        /// Event called when input context is created, for handler setup on other threads, ex. Avalonia UI Thread.
        /// </summary>
        public event Action<IInputContext>? InputContextCreated;

        /// <summary>
        /// Called when the renderer window focus has changed.
        /// </summary>
        public static event Action<bool>? RenderWindowFocusd;

        /// <summary>
        /// The current debug mode of the render pipeline.
        /// </summary>
        public DebugMode debugMode = DebugMode.None;

        /// <summary>
        /// Buffer of all of the object IDs per pixel on screen.
        /// </summary>
        public uint2[]? ObjectIDs { get; private set; }

        /// <summary>
        /// Buffer of all the editor handle IDs overlayed per pixel on screen.
        /// </summary>
        public uint[]? HandleIds { get; private set; }

        /// <summary>
        /// Buffer of all the icon IDs overlayed per pixel on screen.
        /// </summary>
        public uint[]? IconIds { get; private set; }

        /// <summary>
        /// Buffer of all the custom shape IDs overlayed per pixel on screen.
        /// </summary>
        public uint[]? CustomShapeIds { get; private set; }

        // Render texture storage
        private ReadWriteTexture2D<float4>? renderTex;
        private ReadWriteTexture2D<float4>? depthNormalsTex;
        private ReadOnlyTexture2D<float4>? testBackgroundTex;
        private ReadWriteBuffer<uint2>? objectIdBuffer;
        private ReadWriteBuffer<uint>? handleIdBuffer;

        // Buffer storage
        private ReadOnlyBuffer<SDFObjectDTO>? sdfObjBuffer;
        private ReadOnlyBuffer<SDFLightDTO>? lightsBuffer;
        private ReadOnlyBuffer<uint>? textureBuffer;
        private ReadOnlyBuffer<TextureMetadata>? textureMetaBuffer;

        // Terrain storage
        private readonly ReadOnlyBuffer<TerrainData>? terrainHeightBuffer;
        private readonly ReadOnlyBuffer<TerrainMetadata>? terrainMetadataBuffer;

        /// <summary>
        /// Rendered pixels buffer.
        /// </summary>
        public float4[]? Pixels { get; private set; }

        /// <summary>
        /// Depth and world normals (r - depth, gba - normalized world normal vector).
        /// </summary>
        public float4[]? DepthNormalPixels { get; private set; }

        /// <summary>
        /// Is checkerboard rendering enabled (renders half the pixels each frame).
        /// </summary>
        public static bool CheckerboardRenderingEnabled => EngineSettings.Instance?.CheckerboardRendering ?? false;

        // Time measurement

        /// <summary>
        /// Time the renderer has elapsed.
        /// </summary>
        public static double Time { get; private set; }

        /// <summary>
        /// Time between frames.
        /// </summary>
        public static double DeltaTime { get; private set; }

        // Handles
        private float3? editorHandlePosition = null;
        private float editorHandleScale = 1.0f;
        private uint currentHoveredHandle = 0;

        // Icons
        private readonly Dictionary<uint, (float3 position, IconType iconType, float3 direction)> iconsToRender = [];
        private ReadWriteBuffer<uint>? iconIdBuffer;

        // Custom Shapes
        private readonly Dictionary<uint, HandleShape> customShapes = [];
        private ReadWriteBuffer<uint>? customShapeIdBuffer;        

        // Denoising
        private ReadOnlyBuffer<float>? kernelBuffer; // Reconstruction kernel
        private ReadWriteTexture2D<float4>? denoisedTex;

        // Input context
        private IInputContext? inputContext;

        // Textures
        private bool rebuildTextureBuffer = true;

        /// <summary>
        /// Create a new render pipeline.
        /// </summary>
        public RenderPipeline() => Instance = this;

        /// <summary>
        /// Binds the WorldManager.CurrentWorld to this render pipeline.
        /// </summary>
        /// <returns>If the world was successfully bound</returns>
        public bool BindCurrentWorld()
        {
            if (WorldManager.CurrentWorld != null)
            {
                boundWorld = WorldManager.CurrentWorld;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the DPI scaling factor for input coordinate conversion.
        /// </summary>
        public void UpdateDpiScale(float scale)
        {
            currentDpiScale = scale;
        }

        #region editorDrawing

        /// <summary>
        /// Sets the position where editor handles should be rendered.
        /// </summary>
        public void ShowHandles(float3 position, float scale = 1.0f)
        {
            editorHandlePosition = position;
            editorHandleScale = scale;
        }

        /// <summary>
        /// Hides the editor handles.
        /// </summary>
        public void HideHandles()
        {
            editorHandlePosition = null;
        }

        public uint GetHandleAtPosition(int screenX, int screenY)
        {
            if (RendererWindow == null || HandleIds == null || screenX < 0 || screenY < 0 ||
                screenX >= RendererWindow?.Size.X || screenY >= RendererWindow?.Size.Y)
                return 0;

            int width = RendererWindow!.Size.X;
            return HandleIds[screenX + (RendererWindow.Size.Y - screenY) * width];
        }

        public uint GetIconAtPosition(int screenX, int screenY)
        {
            if (RendererWindow == null || iconIdBuffer == null || IconIds == null || screenX < 0 || screenY < 0 ||
                screenX >= RendererWindow.Size.X || screenY >= RendererWindow.Size.Y)
                return 0;

            int width = RendererWindow.Size.X;
            int height = RendererWindow.Size.Y;
            return IconIds[screenX + (height - 1 - screenY) * width];
        }

        public uint GetCustomShapeAtPosition(int screenX, int screenY)
        {
            if (RendererWindow == null || customShapeIdBuffer == null || CustomShapeIds == null || screenX < 0 || screenY < 0 ||
                screenX >= RendererWindow.Size.X || screenY >= RendererWindow.Size.Y)
                return 0;

            int width = RendererWindow.Size.X;
            int height = RendererWindow.Size.Y;
            return CustomShapeIds[screenX + (height - 1 - screenY) * width];
        }

        public void UpdateHoveredHandle(int mouseX, int mouseY)
        {
            lock (SyncLock)
            {
                if (HandleIds == null || RendererWindow == null) return;

                int width = RendererWindow.Size.X;
                int height = RendererWindow.Size.Y;

                if (mouseX >= 0 && mouseX < width && mouseY >= 0 && mouseY < height)
                    currentHoveredHandle = HandleIds[mouseX + (height - 1 - mouseY) * width];
                else currentHoveredHandle = 0;
            }
        }

        public void ShowIcon(float3 position, IconType icon, float3 direction, uint entityId)
        {
            lock (SyncLock)
            {
                iconsToRender[entityId] = (position, icon, direction);
            }
        }

        public void HideIcon(uint entityId)
        {
            lock (SyncLock)
            {
                iconsToRender.Remove(entityId);
            }
        }

        public void ClearIcons()
        {
            lock (SyncLock)
            {
                iconsToRender.Clear();
            }
        }

        #endregion editorDrawing

        /// <summary>
        /// Initializes and runs the render window.
        /// </summary>
        /// <param name="defaultFPS">The default fallback FPS if engine settings cannot be located</param>
        /// <param name="editorMode">If the render pipeline is running in editor mode</param>
        /// <remarks>This method creates a render window with default options, sets up event handlers for
        /// loading and rendering.</remarks>
        public void Run(double defaultFPS, bool editorMode)
        {
            try
            {
                WindowOptions options = WindowOptions.Default;
                if (editorMode)
                {
                    options.TopMost = true;
                    options.WindowBorder = WindowBorder.Hidden;
                }
                options.Title = "SDF Scene";
                options.IsVisible = true;
                options.VSync = true;
                options.ShouldSwapAutomatically = true;

                if (EngineSettings.Instance.MaxFPS > 0)
                    options.UpdatesPerSecond = EngineSettings.Instance.MaxFPS;
                else options.UpdatesPerSecond = defaultFPS;

                // Load render window settings
                options.Size = new Silk.NET.Maths.Vector2D<int>(EngineSettings.Instance.ResolutionWidth, EngineSettings.Instance.ResolutionHeight);

                RendererWindow = Window.Create(options);
                Debug.Info($"Renderer: Created Render Window on thread {System.Environment.CurrentManagedThreadId}");

                closeWindowWithCloseEvent = true;
                RendererWindow.Load += OnLoad;
                RendererWindow.Render += OnRender;
                RendererWindow.Closing += OnClosing;
                RendererWindow.FocusChanged += (f) => { if (RenderWindowFocusd != null) RenderWindowFocusd!(f); };

                // Handle texture updates
                TextureSystem.UpdatedTextureData += () => rebuildTextureBuffer = true;
                rebuildTextureBuffer = true;

                Debug.Info("Renderer: Starting window run loop");
                RendererWindow.Run();
            }
            catch (Exception ex)
            {
                Debug.Error($"Renderer: Failed to run window", ex);
                throw;
            }
        }

        /// <summary>
        /// Called when the renderer window is closing.
        /// </summary>
        private void OnClosing()
        {
            lock (SyncLock)
            {
                CleanupResources();
                if (closeWindowWithCloseEvent) Close?.Invoke(); // Invoke the close event if there are any subscribers
            }
        }

        /// <summary>
        /// Stops the renderer window from running.
        /// </summary>
        /// <returns>If the window was successfully closed</returns>
        public bool Stop()
        {
            lock (SyncLock)
            {
                if (RendererWindow != null)
                {
                    closeWindowWithCloseEvent = false;
                    RendererWindow!.Close();
                    RendererWindow = null;
                    closeWindowWithCloseEvent = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Called on render window load.
        /// </summary>
        private void OnLoad()
        {
            lock (SyncLock)
            {
                if (RendererWindow == null) return;
                gl = GL.GetApi(RendererWindow);

                // Initial focus hard set to avoid cascading events
                RenderWindowFocusd?.Invoke(true);

                // Initialize OpenGL context
                Debug.Info("Renderer: Initialize OpenGL Context");
                gl.GenTextures(1, out glTexture);
                gl.BindTexture(TextureTarget.Texture2D, glTexture);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
                gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

                // Load graphics device
                Device = GraphicsDevice.GetDefault();
                Device.DeviceLost += Device_DeviceLost;

                Debug.Info("Renderer: Compiling OpenGL Shader Program");
                glShaderProgram = CompileShaders();
                gl!.GenVertexArrays(1, out uint vao);
                gl.BindVertexArray(vao);
                Debug.Info("Renderer: VAO Bound");

                try
                {
                    Debug.Info("Renderer: Creating input context...");
                    inputContext ??= RendererWindow.CreateInput(); // Must create input context on same thread as render window!
                    if (inputContext != null)
                    {
                        Debug.Info("Renderer: Input context created successfully");
                        InputContextCreated?.Invoke(inputContext);
                    }
                    else Debug.Error("Renderer: Failed to create input context");
                }
                catch (Exception ex)
                {
                    Debug.Error($"Renderer: Exception creating input context", ex);
                }
                InputReady = true;
            }
        }

        /// <summary>
        /// Debug and attempt to rebuild on device lost.
        /// </summary>
        private void Device_DeviceLost(object? sender, DeviceLostEventArgs e)
        {
            deviceLost = true;
            Debug.Error($"Renderer: Graphics Device Lost!\nCause: {e.Reason}");
            lock (SyncLock)
            {
                Device = null;
                CleanupResources();
            }
        }

        /// <summary>
        /// Executes the render pipeline frame.
        /// </summary>
        /// <param name="delta">Travel delta between frames in seconds</param>
        private void OnRender(double delta)
        {
            DeltaTime = delta; // Track frame time
            Time += delta;

            if (Device == null || deviceLost)
            {
                Debug.Warning("Renderer: Device lost, skipping rendering");
                return;
            }

            if (boundWorld == null) return;
            boundWorld.CallRender();
            int texWidth = 0, texHeight = 0;
            IWindow? window;
            lock (SyncLock)
            {
                if (RendererWindow == null || RendererWindow.IsClosing) return;

                // Apply render window settings
                RendererWindow.VSync = EngineSettings.Instance.VSync;
                RendererWindow.FramesPerSecond = EngineSettings.Instance.MaxFPS;
                window = RendererWindow;
            }

            try
            {
                texWidth = window.Size.X;
                texHeight = window.Size.Y;
                if (texWidth < 1 || texHeight < 1) return;
            }
            catch (NullReferenceException ex)
            {
                Debug.Warning($"Renderer: Render window lost, skipping rendering", ex);
                return;
            }

            SDFWorldDTO worldDTO;
            SDFObjectDTO[] sdfObjDTO;
            SDFLightDTO[] sdfLightsDTO;
            lock (SyncLock)
            {
                worldDTO = SDFRenderSystem.PreparedWorldDTO;
                sdfObjDTO = SDFRenderSystem.PreparedSDFObjectsDTO;
                sdfLightsDTO = SDFRenderSystem.PreparedLightsDTO;
            }
            if (sdfObjDTO.Length < 1) return;

            try
            {
                lock (SyncLock)
                {
                    // Build render texture
                    if (renderTex == null || renderTex.Width != texWidth || renderTex.Height != texHeight || Pixels == null)
                    {
                        renderTex?.Dispose();
                        renderTex = Device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                        Pixels = new float4[texWidth * texHeight];
                    }

                    // Build denoised texture (for post-process)
                    if (denoisedTex == null || denoisedTex.Width != texWidth || denoisedTex.Height != texHeight)
                    {
                        denoisedTex?.Dispose();
                        denoisedTex = Device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                    }

                    // Build test background texture
                    if (testBackgroundTex == null)
                    {
                        testBackgroundTex?.Dispose();
                        testBackgroundTex = Device!.AllocateReadOnlyTexture2D<float4>(texWidth, texHeight);
                    }

                    // Build depth and normal texture
                    if (depthNormalsTex == null || depthNormalsTex.Width != texWidth || depthNormalsTex.Height != texHeight || DepthNormalPixels == null)
                    {
                        depthNormalsTex?.Dispose();
                        depthNormalsTex = Device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                        DepthNormalPixels = new float4[texWidth * texHeight];
                    }

                    // Build object ID buffer
                    if (objectIdBuffer == null || objectIdBuffer.Length != (texWidth * texHeight) || ObjectIDs == null)
                    {
                        objectIdBuffer?.Dispose();
                        objectIdBuffer = Device!.AllocateReadWriteBuffer<uint2>(texWidth * texHeight);
                        ObjectIDs = new uint2[texWidth * texHeight];
                    }

                    // Build handle ID buffer
                    if (handleIdBuffer == null || handleIdBuffer.Length != (texWidth * texHeight) || HandleIds == null)
                    {
                        handleIdBuffer?.Dispose();
                        handleIdBuffer = Device!.AllocateReadWriteBuffer<uint>(texWidth * texHeight);
                        HandleIds = new uint[texWidth * texHeight];
                    }

                    // Build custom shape ID buffer
                    if (customShapeIdBuffer == null || customShapeIdBuffer.Length != (texWidth * texHeight))
                    {
                        customShapeIdBuffer?.Dispose();
                        customShapeIdBuffer = Device!.AllocateReadWriteBuffer<uint>(texWidth * texHeight);
                        CustomShapeIds = new uint[texWidth * texHeight];
                    }

                    // Build icon ID buffer
                    if (iconIdBuffer == null || iconIdBuffer.Length != (texWidth * texHeight))
                    {
                        iconIdBuffer?.Dispose();
                        iconIdBuffer = Device!.AllocateReadWriteBuffer<uint>(texWidth * texHeight);
                        IconIds = new uint[texWidth * texHeight];
                    }

                    // Build kernel buffer
                    if (kernelBuffer == null || ObjectIDs == null)
                    {
                        kernelBuffer?.Dispose();
                        kernelBuffer = Device!.AllocateReadOnlyBuffer([1.0f / 16.0f, 1.0f / 4.0f, 3.0f / 8.0f, 1.0f / 4.0f, 1.0f / 16.0f]);
                    }

                    // Build and copy buffers
                    if (sdfObjBuffer?.Length != sdfObjDTO.Length)
                    {
                        sdfObjBuffer?.Dispose();
                        sdfObjBuffer = Device?.AllocateReadOnlyBuffer(sdfObjDTO);
                    }
                    else sdfObjBuffer.CopyFrom(sdfObjDTO);
                    if (sdfObjBuffer == null) return;

                    if (lightsBuffer?.Length != sdfLightsDTO.Length)
                    {
                        lightsBuffer?.Dispose();
                        lightsBuffer = Device?.AllocateReadOnlyBuffer(sdfLightsDTO);
                    }
                    else lightsBuffer.CopyFrom(sdfLightsDTO);
                    if (lightsBuffer == null) return;

                    if (rebuildTextureBuffer && TextureSystem.AllTextureData != null && TextureSystem.AllTextureMetadata != null)
                    {
                        textureBuffer?.Dispose();
                        textureBuffer = Device?.AllocateReadOnlyBuffer(TextureSystem.AllTextureData);
                        if (textureBuffer == null) return;

                        textureMetaBuffer?.Dispose();
                        textureMetaBuffer = Device?.AllocateReadOnlyBuffer(TextureSystem.AllTextureMetadata);
                        if (textureMetaBuffer == null) return;

                        TextureSystem.FreeCPUTextureData();
                        rebuildTextureBuffer = false;
                    }

                    // Dispatch SDF compute shader
                    int outputMode = 0;
                    if ((int)debugMode > 3) outputMode = (int)debugMode - 3;

                    // Checkerboard rendering toggle
                    int checkerboardEnabled = CheckerboardRenderingEnabled ? 1 : 0;
                    int dispatchWidth, dispatchHeight;
                    if (CheckerboardRenderingEnabled)
                    {
                        dispatchWidth = (texWidth + 1) / 2;
                        dispatchHeight = texHeight;
                    }
                    else
                    {
                        dispatchWidth = texWidth;
                        dispatchHeight = texHeight;
                    }

                    // Quickly update the world data buffer
                    SDFRenderSystem.UploadWorldData(Device!);
                    SDFShader3D shader = new SDFShader3D
                        (texWidth,
                        texHeight,
                        texWidth / (float)texHeight,
                        TimeSystem.FrameCount,
                        (int)debugMode,
                        checkerboardEnabled,
                        SDFRenderSystem.WorldDataBuffer!,
                        renderTex,
                        depthNormalsTex,
                        objectIdBuffer,
                        sdfObjBuffer,
                        lightsBuffer,
                        textureBuffer!,
                        textureMetaBuffer!);
                    Device?.For(dispatchWidth, dispatchHeight, shader);
                }

                // Rendering pipeline
                ReadWriteTexture2D<float4>? currentTexture = renderTex;

                // Division Denoising
                if (worldDTO.enableDivisionDenoise == 1 && debugMode == DebugMode.None &&
                    currentTexture != null && denoisedTex != null && objectIdBuffer != null && depthNormalsTex != null)
                {
                    lock (SyncLock)
                    {
                        DivisionDenoiseShader denoiseShader = new DivisionDenoiseShader(
                            texWidth,
                            texHeight,
                            worldDTO.divisionThreshold,
                            worldDTO.divisionDomain,
                            currentTexture,
                            denoisedTex,
                            depthNormalsTex,
                            sdfObjBuffer,
                            objectIdBuffer);
                        Device?.For(texWidth, texHeight, denoiseShader);
                    }
                    currentTexture = denoisedTex;
                }

                // A-Trous denoising using wavelets
                if (worldDTO.enableATrousDenoise == 1 && debugMode == DebugMode.None &&
                    currentTexture != null && denoisedTex != null && kernelBuffer != null && objectIdBuffer != null && depthNormalsTex != null)
                {
                    ReadWriteTexture2D<float4> ping = currentTexture;
                    ReadWriteTexture2D<float4> pong = denoisedTex;
                    int stepSize = 1;

                    for (int i = 0; i < worldDTO.aTrousStepCount; i++)
                    {
                        lock (SyncLock)
                        {
                            ATrousDenoiseShader aTrousShader = new ATrousDenoiseShader(
                                texWidth,
                                texHeight,
                                stepSize,
                                ping,
                                pong,
                                depthNormalsTex,
                                sdfObjBuffer,
                                objectIdBuffer,
                                kernelBuffer);
                            Device?.For(texWidth, texHeight, aTrousShader);
                        }

                        // Swap buffers for next pass
                        (ping, pong) = (pong, ping);
                        stepSize *= 2;
                    }
                    currentTexture = ping; // Final result
                }

                foreach (var (_, transform, camera) in W.QueryData<Transform, Camera>())
                {
                    // Depth of field
                    if (debugMode == DebugMode.None && camera.enableDepthOfField &&
                        currentTexture != null && denoisedTex != null && objectIdBuffer != null && depthNormalsTex != null)
                    {
                        ReadWriteTexture2D<float4> source = currentTexture;
                        ReadWriteTexture2D<float4> target = denoisedTex;
                        lock (SyncLock)
                        {
                            FastDepthOfFieldShader dofShader = new FastDepthOfFieldShader(
                                texWidth,
                                texHeight, // Max blur radius
                                source,
                                target,
                                depthNormalsTex,
                                objectIdBuffer,
                                sdfObjBuffer,
                                worldDTO);
                            Device?.For(texWidth, texHeight, dofShader);
                        }
                        currentTexture = target;
                    }
                    break; // Use first camera
                }

                // Editor handles
                if (editorHandlePosition.HasValue)
                {
                    lock (SyncLock)
                    {
                        EditorHandleShader handleShader = new EditorHandleShader(
                            texWidth, texHeight,
                            texWidth / (float)texHeight, // aspect ratio
                            worldDTO.camScreenDist,
                            worldDTO.cameraOrigin,
                            worldDTO.camForward,
                            worldDTO.camRight,
                            worldDTO.camUp,
                            currentTexture!,
                            handleIdBuffer,
                            editorHandlePosition.Value,
                            editorHandleScale,
                            currentHoveredHandle);
                        Device?.For(texWidth, texHeight, handleShader);
                    }
                }

                // Custom shapes (bounding boxes, etc.)
                if (customShapes.Count > 0)
                {
                    lock (SyncLock)
                    {
                        foreach (var (shapeId, shape) in customShapes)
                        {
                            CustomShapeShader shapeShader = new CustomShapeShader(
                                texWidth, texHeight,
                                texWidth / (float)texHeight,
                                worldDTO.camScreenDist,
                                worldDTO.cameraOrigin,
                                worldDTO.camForward,
                                worldDTO.camRight,
                                worldDTO.camUp,
                                currentTexture!,
                                customShapeIdBuffer,
                                shape,
                                shapeId);
                            Device?.For(texWidth, texHeight, shapeShader);
                        }
                    }
                }

                // Editor Icons
                if (iconsToRender.Count > 0)
                {
                    lock (SyncLock)
                    {
                        foreach (var (entityId, (position, icon, direction)) in iconsToRender)
                        {
                            IconShader iconShader = new IconShader(
                                texWidth, texHeight,
                                texWidth / (float)texHeight,
                                worldDTO.camScreenDist,
                                worldDTO.cameraOrigin,
                                worldDTO.camForward,
                                worldDTO.camRight,
                                worldDTO.camUp,
                                currentTexture!,
                                iconIdBuffer,
                                position,
                                (uint)icon,
                                direction, // Cast the IconType enum to uint
                                entityId);    // Use icon type as ID
                            Device?.For(texWidth, texHeight, iconShader);
                        }
                    }
                }

                // Copy final result for OpenGL display
                depthNormalsTex?.CopyTo(DepthNormalPixels!);
                objectIdBuffer?.CopyTo(ObjectIDs!);
                handleIdBuffer?.CopyTo(HandleIds!);
                iconIdBuffer?.CopyTo(IconIds!);
                customShapeIdBuffer?.CopyTo(CustomShapeIds!);
                currentTexture?.CopyTo(Pixels!);
            }
            catch (ObjectDisposedException ex)
            {
                Debug.Warning($"Renderer: Object disposed during rendering", ex);
                CleanupResources();
                return;
            }
            catch (InvalidOperationException ex)
            {
                Debug.Error($"Renderer: Invalid operation during ComputeSharp execution", ex);
                Device = GraphicsDevice.GetDefault();
                return;
            }

            // Push final texture to OpenGL
            unsafe
            {
                fixed (float4* dataPtr = Pixels)
                {
                    gl!.BindTexture(TextureTarget.Texture2D, glTexture);
                    gl.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        (int)InternalFormat.Rgba32f,
                        (uint)texWidth,
                        (uint)texHeight,
                        0,
                        PixelFormat.Rgba,
                        PixelType.Float,
                        dataPtr);
                }
            }

            gl.Viewport(0, 0, (uint)texWidth, (uint)texHeight);
            gl.ClearColor(0f, 0f, 0f, 1f);
            gl.Clear((uint)ClearBufferMask.ColorBufferBit);
            gl.UseProgram(glShaderProgram);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, glTexture);
            int loc = gl.GetUniformLocation(glShaderProgram, "tex");
            gl.Uniform1(loc, 0);

            gl.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            gl.Finish();
        }

        /// <summary>
        /// Compiles and links vertex and fragment shaders into a shader program.
        /// </summary>
        /// <returns>The handle of the compiled and linked shader program.</returns>
        private uint CompileShaders()
        {
            string vertexSrc = """
            #version 330 core
            out vec2 uv;

            void main() {
                const vec2 positions[4] = vec2[](
                    vec2(-1, -1),
                    vec2( 1, -1),
                    vec2(-1,  1),
                    vec2( 1,  1)
                );
                const vec2 uvs[4] = vec2[](
                    vec2(0, 0),
                    vec2(1, 0),
                    vec2(0, 1),
                    vec2(1, 1)
                );
                gl_Position = vec4(positions[gl_VertexID], 0, 1);
                uv = uvs[gl_VertexID];
            }
            """;

            string fragSrc = """
            #version 330 core
            in vec2 uv;
            out vec4 fragColor;
            uniform sampler2D tex;

            void main() {
                fragColor = texture(tex, uv);
            }
            """;

            uint vs = gl!.CreateShader(ShaderType.VertexShader);
            gl.ShaderSource(vs, vertexSrc);
            gl.CompileShader(vs);
            CheckShaderCompileStatus(vs, "Vertex Shader");

            uint fs = gl.CreateShader(ShaderType.FragmentShader);
            gl.ShaderSource(fs, fragSrc);
            gl.CompileShader(fs);
            CheckShaderCompileStatus(fs, "Fragment Shader");

            uint shader = gl.CreateProgram();
            gl.AttachShader(shader, vs);
            gl.AttachShader(shader, fs);
            gl.LinkProgram(shader);
            CheckProgramLinkStatus(shader);

            gl.DeleteShader(vs);
            gl.DeleteShader(fs);
            return shader;
        }

        /// <summary>
        /// Checks the compile status of a shader and debugs the result.
        /// </summary>
        /// <remarks>If the shader compilation fails, the method retrieves the error log and writes it to
        /// the debug output. If the compilation succeeds, a success message is written to the debug output.</remarks>
        /// <param name="shader">The identifier of the shader to check.</param>
        /// <param name="name">The name of the shader, used for logging purposes.</param>
        private void CheckShaderCompileStatus(uint shader, string name)
        {
            gl!.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
            if (success == 0)
            {
                string info = gl.GetShaderInfoLog(shader);
                Debug.Error($"{name} Compile Error: {info}");
            }
            else Debug.Info($"{name} Compiled Successfully");
        }

        /// <summary>
        /// Checks the link status of the OpenGL shader program and debugs the result.
        /// </summary>
        /// <remarks>If the shader program fails to link, the method retrieves and debugs the program's
        /// error information. If the shader program links successfully, a success message is displayed.</remarks>
        /// <param name="program">The identifier of the shader program to check.</param>
        private void CheckProgramLinkStatus(uint program)
        {
            gl!.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
            if (success == 0)
            {
                string info = gl.GetProgramInfoLog(program);
                Debug.Error($"Shader Program Link Error: {info}");
            }
            else Debug.Info("Shader Program Linked Successfully");
        }

        /// <summary>
        /// Disposes of all managed memory and sets to null.
        /// </summary>
        private void CleanupResources()
        {
            renderTex?.Dispose();
            denoisedTex?.Dispose();
            depthNormalsTex?.Dispose();
            objectIdBuffer?.Dispose();
            sdfObjBuffer?.Dispose();
            lightsBuffer?.Dispose();
            kernelBuffer?.Dispose();
            inputContext?.Dispose(); // Rare null reference error here

            renderTex = null;
            denoisedTex = null;
            depthNormalsTex = null;
            objectIdBuffer = null;
            sdfObjBuffer = null;
            lightsBuffer = null;
            kernelBuffer = null;
            inputContext = null;
        }
    }
}