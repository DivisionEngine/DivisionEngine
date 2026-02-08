using ComputeSharp;
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
        public enum DebugMode
        {
            None = 0, Depth = 1, WorldNormals = 2, ObjectID = 3, RaySteps = 4, Shadows = 5, BRDF = 6
        }

        // Special variables
        public readonly Lock SyncLock = new Lock(); // Synchronization lock for thread safety
        private bool closeWindowWithCloseEvent = true;

        // OpenGL variables
        private GL? gl;
        private GraphicsDevice? device; // Graphics device for ComputeSharp operations
        private uint glTexture, glShaderProgram;

        // Rendering variables
        public IWindow? RendererWindow;
        public bool InputReady { get; private set; } = false; // Indicates if the renderer is ready to process input
        public event Action? Close; // Event to handle window close actions
        public event Action<IInputContext>? InputContextCreated; // Event called when input context is created, for handler setup on other threads (Avalonia)
        public DebugMode debugMode = DebugMode.None; // Current debug mode, if any

        // Render texture storage
        private ReadWriteTexture2D<float4>? renderTex;
        private ReadWriteTexture2D<float4>? depthNormalsTex;
        private ReadWriteBuffer<int>? objectIdBuffer;

        // Buffer storage
        private ReadOnlyBuffer<SDFWorldDTO>? worldBuffer;
        private ReadOnlyBuffer<SDFPrimitiveObjectDTO>? primitivesBuffer;
        private ReadOnlyBuffer<SDFLightDTO>? lightsBuffer;
        private float4[]? pixels;
        private float4[]? depthNormalPixels;
        private int[]? objectIDs;

        // World variables
        public float Time;
        public World? boundWorld;

        // Denoising
        private ReadWriteTexture2D<float4>? denoisedTex;
        private ReadOnlyBuffer<int>? objectIdReadOnlyBuffer; // Denoise needs ReadOnly version

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
        /// Initializes and runs the render window.
        /// </summary>
        /// <remarks>This method creates a render window with default options, sets up event handlers for
        /// loading and rendering.</remarks>
        public void Run(double requestedFPS, bool editorMode)
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
                options.UpdatesPerSecond = requestedFPS;

                RendererWindow = Window.Create(options);
                Debug.Info($"Renderer: Created Render Window on thread {Environment.CurrentManagedThreadId}");

                closeWindowWithCloseEvent = true;
                RendererWindow.Load += OnLoad;
                RendererWindow.Render += OnRender;
                RendererWindow.Closing += OnClosing;

                Debug.Info("Renderer: Starting window run loop");
                RendererWindow.Run();
            }
            catch (Exception ex)
            {
                Debug.Error($"Renderer: Failed to run window - {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
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
                device?.Dispose();
                renderTex?.Dispose();
                denoisedTex?.Dispose();
                depthNormalsTex?.Dispose();
                objectIdBuffer?.Dispose();
                objectIdReadOnlyBuffer?.Dispose();
                worldBuffer?.Dispose();
                primitivesBuffer?.Dispose();
                lightsBuffer?.Dispose();
                device = null;
                renderTex = null;
                denoisedTex = null;
                depthNormalsTex = null;
                objectIdBuffer = null;
                objectIdReadOnlyBuffer = null;
                worldBuffer = null;
                primitivesBuffer = null;
                lightsBuffer = null;
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
            gl = GL.GetApi(RendererWindow);

            // Initialize OpenGL context
            Debug.Info("Renderer: Initialize OpenGL Context");
            gl.GenTextures(1, out glTexture);
            gl.BindTexture(TextureTarget.Texture2D, glTexture);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            // Load graphics device
            device = GraphicsDevice.GetDefault();
            device.DeviceLost += Device_DeviceLost;

            Debug.Info("Renderer: Compiling OpenGL Shader Program");
            glShaderProgram = CompileShaders();
            gl!.GenVertexArrays(1, out uint vao);
            gl.BindVertexArray(vao);
            Debug.Info("Renderer: VAO Bound");

            try
            {
                Debug.Info("Renderer: Creating input context...");
                IInputContext? inputContext = RendererWindow!.CreateInput(); // Must create input context on same thread as render window!
                if (inputContext != null)
                {
                    Debug.Info("Renderer: Input context created successfully");
                    InputContextCreated?.Invoke(inputContext);
                }
                else Debug.Error("Renderer: Failed to create input context");
            }
            catch (Exception ex)
            {
                Debug.Error($"Renderer: Exception creating input context: {ex.Message}");
            }
            InputReady = true;
        }

        /// <summary>
        /// Debug and attempt to rebuild on device lost.
        /// </summary>
        private void Device_DeviceLost(object? sender, DeviceLostEventArgs e)
        {
            Debug.Error($"Renderer: Graphics Device Lost!\nCause: {e.Reason}");
            try { device = GraphicsDevice.GetDefault(); }
            catch(Exception ex) { Debug.Error($"Renderer: Could not get default graphics device: {ex.Message}"); }
        }

        /// <summary>
        /// Called when the frame must be rendered.
        /// </summary>
        /// <param name="delta">Window delta</param>
        /*private void OnRender(double delta)
        {
            boundWorld?.CallRender(); // Calls the render loop on the bound world

            // Variable setup (variables modified outside of renderer must be locked)
            int texWidth = RendererWindow!.Size.X, texHeight = RendererWindow.Size.Y;
            if (texWidth < 1 || texHeight < 1) return; // Ensure valid texture dimensions

            // Gather SDF world information
            SDFWorldDTO worldDTO;
            SDFPrimitiveObjectDTO[] sdfPrimitivesDTO;
            SDFLightDTO[] sdfLightsDTO;
            lock (SyncLock)
            {
                worldDTO = SDFRenderSystem.PreparedWorldDTO;
                sdfPrimitivesDTO = SDFRenderSystem.PreparedPrimitivesDTO;
                sdfLightsDTO = SDFRenderSystem.PreparedLightsDTO;
            }

            // Check if buffers will be null
            if (sdfPrimitivesDTO.Length < 1) return;
            //if (sdfLightsDTO.Length < 1) return;

            // Check if device is disposed
            if (device == null)
            {
                Debug.Warning("Renderer: GraphicsDevice is null or disposed");
                return;
            }

            try
            {
                // Build render texture
                if (renderTex == null || renderTex.Width != texWidth || renderTex.Height != texHeight || pixels == null)
                {
                    renderTex?.Dispose();
                    renderTex = device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                    pixels = new float4[texWidth * texHeight];
                }

                // Build depth and normal texture
                if (depthNormalsTex == null || depthNormalsTex.Width != texWidth || depthNormalsTex.Height != texHeight || depthNormalPixels == null)
                {
                    depthNormalsTex?.Dispose();
                    depthNormalsTex = device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                    depthNormalPixels = new float4[texWidth * texHeight];
                }

                // Build objectIdBuffer
                if (objectIdBuffer == null || objectIdBuffer.Length != (texWidth * texHeight) || objectIDs == null)
                {
                    objectIdBuffer?.Dispose();
                    objectIdBuffer = device!.AllocateReadWriteBuffer<int>(texWidth * texHeight);
                    objectIDs = new int[texWidth * texHeight];
                }

                // Build and copy buffers
                worldBuffer ??= device!.AllocateReadOnlyBuffer<SDFWorldDTO>(1);
                worldBuffer.CopyFrom([worldDTO]);

                primitivesBuffer?.Dispose();
                primitivesBuffer = device?.AllocateReadOnlyBuffer(sdfPrimitivesDTO);
                if (primitivesBuffer == null) return;
                //lightsBuffer?.Dispose();
                //lightsBuffer = device?.AllocateReadOnlyBuffer(sdfLightsDTO);
                //if (lightsBuffer == null) return;

                // Dispatch SDF compute shader
                int outputMode = 0;
                if ((int)debugMode > 3) outputMode = (int)debugMode - 3;
                SDFShader3D shader = new SDFShader3D(texWidth, texHeight, outputMode,
                    renderTex, depthNormalsTex, objectIdBuffer, worldBuffer, primitivesBuffer);
                device?.For(texWidth, texHeight, shader);

                depthNormalsTex?.CopyTo(depthNormalPixels!); // In the future only activate when debugging or in use for effects
                objectIdBuffer?.CopyTo(objectIDs!); // In the future only activate when debugging or in use for effects
                lock (SyncLock)
                {
                    if ((int)debugMode > 0 && (int)debugMode < 4 && renderTex != null && depthNormalsTex != null && objectIdBuffer != null)
                    {
                        SDFDebug3D debugShader = new SDFDebug3D(renderTex, depthNormalsTex, objectIdBuffer,
                            (int)debugMode, texWidth);
                        device?.For(texWidth, texHeight, debugShader); // Call debug visulization shader
                    }
                }
                renderTex?.CopyTo(pixels!); // Copy rendered result to CPU
            }
            catch (ObjectDisposedException ex)
            {
                Debug.Warning($"Renderer: Object disposed during rendering: {ex.Message}");
                renderTex?.Dispose(); // Reinitialize buffers on next frame
                depthNormalsTex?.Dispose();
                objectIdBuffer?.Dispose();
                worldBuffer?.Dispose();
                primitivesBuffer?.Dispose();
                lightsBuffer?.Dispose();
                device = null;
                renderTex = null;
                depthNormalsTex = null;
                objectIdBuffer = null;
                worldBuffer = null;
                primitivesBuffer = null;
                lightsBuffer = null;
                return;
            }
            catch (InvalidOperationException ex)
            {
                Debug.Error($"Renderer: Invalid operation during ComputeSharp execution: {ex.Message}");
                return;
            }

            // Push compute texture to openGL rendered quad (via Silk.Net)
            unsafe
            {
                fixed (float4* dataPtr = pixels) // This should be set to whatever debug mode is currently active
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
        }*/

        private void OnRender(double delta)
        {
            boundWorld?.CallRender();
            int texWidth = RendererWindow!.Size.X, texHeight = RendererWindow.Size.Y;
            if (texWidth < 1 || texHeight < 1) return;

            SDFWorldDTO worldDTO;
            SDFPrimitiveObjectDTO[] sdfPrimitivesDTO;
            SDFLightDTO[] sdfLightsDTO;
            lock (SyncLock)
            {
                worldDTO = SDFRenderSystem.PreparedWorldDTO;
                sdfPrimitivesDTO = SDFRenderSystem.PreparedPrimitivesDTO;
                sdfLightsDTO = SDFRenderSystem.PreparedLightsDTO;
            }

            if (sdfPrimitivesDTO.Length < 1) return;
            if (device == null)
            {
                Debug.Warning("Renderer: GraphicsDevice is null or disposed");
                return;
            }

            try
            {
                // Build render texture
                if (renderTex == null || renderTex.Width != texWidth || renderTex.Height != texHeight || pixels == null)
                {
                    renderTex?.Dispose();
                    renderTex = device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                    pixels = new float4[texWidth * texHeight];
                }

                // Build denoised texture (for post-process)
                if (denoisedTex == null || denoisedTex.Width != texWidth || denoisedTex.Height != texHeight)
                {
                    denoisedTex?.Dispose();
                    denoisedTex = device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                }

                // Build depth and normal texture
                if (depthNormalsTex == null || depthNormalsTex.Width != texWidth || depthNormalsTex.Height != texHeight || depthNormalPixels == null)
                {
                    depthNormalsTex?.Dispose();
                    depthNormalsTex = device!.AllocateReadWriteTexture2D<float4>(texWidth, texHeight);
                    depthNormalPixels = new float4[texWidth * texHeight];
                }

                // Build objectIdBuffer (ReadWrite for SDF shader)
                if (objectIdBuffer == null || objectIdBuffer.Length != (texWidth * texHeight) || objectIDs == null)
                {
                    objectIdBuffer?.Dispose();
                    objectIdBuffer = device!.AllocateReadWriteBuffer<int>(texWidth * texHeight);
                    objectIDs = new int[texWidth * texHeight];
                }

                // Build and copy buffers
                worldBuffer ??= device!.AllocateReadOnlyBuffer<SDFWorldDTO>(1);
                worldBuffer.CopyFrom([worldDTO]);

                primitivesBuffer?.Dispose();
                primitivesBuffer = device?.AllocateReadOnlyBuffer(sdfPrimitivesDTO);
                if (primitivesBuffer == null) return;

                // Dispatch SDF compute shader
                int outputMode = 0;
                if ((int)debugMode > 3) outputMode = (int)debugMode - 3;
                SDFShader3D shader = new SDFShader3D(texWidth, texHeight, outputMode,
                    renderTex, depthNormalsTex, objectIdBuffer, worldBuffer, primitivesBuffer);
                device?.For(texWidth, texHeight, shader);

                // Handle debug modes
                lock (SyncLock)
                {
                    if ((int)debugMode > 0 && (int)debugMode < 4 && renderTex != null && depthNormalsTex != null && objectIdBuffer != null)
                    {
                        SDFDebug3D debugShader = new SDFDebug3D(renderTex, depthNormalsTex, objectIdBuffer,
                            (int)debugMode, texWidth);
                        device?.For(texWidth, texHeight, debugShader);
                    }
                }

                // Run denoise pass (if enabled and not in debug mode)
                ReadWriteTexture2D<float4>? finalTexture = renderTex;
                if (worldDTO.enableDenoise == 1 && debugMode == DebugMode.None)
                {
                    // Create ReadOnly version of objectIdBuffer for denoise shader
                    objectIdReadOnlyBuffer?.Dispose();
                    objectIdReadOnlyBuffer = device?.AllocateReadOnlyBuffer(objectIDs!);

                    if (objectIdReadOnlyBuffer != null && denoisedTex != null)
                    {
                        // Convert renderTex to ReadOnlyTexture2D by creating temporary copy
                        // ComputeSharp limitation: need to use Upload pattern
                        var uploadTex = device?.AllocateReadOnlyTexture2D<float4>(texWidth, texHeight);
                        if (uploadTex != null)
                        {
                            renderTex?.CopyTo(pixels!);
                            uploadTex.CopyFrom(pixels!);
                            DenoiseShader denoiseShader = new DenoiseShader(
                                texWidth, texHeight, worldDTO.divisionThreshold,
                                uploadTex, denoisedTex, depthNormalsTex!,
                                primitivesBuffer, objectIdReadOnlyBuffer);

                            device?.For(texWidth, texHeight, denoiseShader);
                            finalTexture = denoisedTex;
                            uploadTex.Dispose();
                        }
                    }
                }

                // Copy final result for output buffers
                depthNormalsTex?.CopyTo(depthNormalPixels!);
                objectIdBuffer?.CopyTo(objectIDs!);
                finalTexture?.CopyTo(pixels!); // Copy denoised or original result
            }
            catch (ObjectDisposedException ex)
            {
                Debug.Warning($"Renderer: Object disposed during rendering: {ex.Message}");
                renderTex?.Dispose();
                denoisedTex?.Dispose();
                depthNormalsTex?.Dispose();
                objectIdBuffer?.Dispose();
                objectIdReadOnlyBuffer?.Dispose();
                worldBuffer?.Dispose();
                primitivesBuffer?.Dispose();
                lightsBuffer?.Dispose();
                device = null;
                renderTex = null;
                denoisedTex = null;
                depthNormalsTex = null;
                objectIdBuffer = null;
                objectIdReadOnlyBuffer = null;
                worldBuffer = null;
                primitivesBuffer = null;
                lightsBuffer = null;
                return;
            }
            catch (InvalidOperationException ex)
            {
                Debug.Error($"Renderer: Invalid operation during ComputeSharp execution: {ex.Message}");
                return;
            }

            // Push final texture to OpenGL
            unsafe
            {
                fixed (float4* dataPtr = pixels)
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
    }

}
