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
using Avalonia;
using Avalonia.Threading;
using DivisionEngine.Rendering;
using DivisionEngine.Systems;
using System;
using System.Threading.Tasks;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Manages render window state (visible, focus, position, etc).
    /// </summary>
    public class RenderWindowManagementSystem : SystemBase
    {
        private double prevWidth, prevHeight; 
        private int prevX, prevY;
        private int forceGrabWindowTimer;
        private int initializeTimer;
        private bool updatingFocus;

        /// <summary>
        /// Editor window is in focus.
        /// </summary>
        public static bool EditorFocused { get; set; }

        /// <summary>
        /// Renderer window is in focus.
        /// </summary>
        public static bool RendererFocused { get; set; }

        public override void Awake()
        {
            prevWidth = 0;
            prevHeight = 0;
            prevX = 0;
            prevY = 0;
            forceGrabWindowTimer = 0;
            initializeTimer = 60;

            EditorFocused = false;
            RendererFocused = true;
            RenderPipeline.RenderWindowFocusd += (f) => RendererFocused = f;
            RenderPipeline.RenderWindowFocusd += async (_) => await FocusUpdate();
            App.AppFocused += (f) => EditorFocused = f;
            App.AppFocused += async (_) => await FocusUpdate();

            // Make sure object selection is enabled and getting called
            EntitySelectionSystem.CanSelect = true;
            EntitySelectionSystem.OnEntitySelected += PropertiesWindow.LoadEntityComponents;
            EntitySelectionSystem.OnNoEntityFound += () => PropertiesWindow.LoadWorldData(WorldManager.CurrentWorld);
        }

        public override void FixedUpdate()
        {
            if (initializeTimer > 0) initializeTimer--;
        }

        public async Task FocusUpdate()
        {
            if (updatingFocus) return;
            updatingFocus = true;
            await Task.Delay(300); // Wait to see if other window is immediately focused

            Dispatcher.UIThread.Post(() =>
            {
                if (initializeTimer == 0 && !EditorFocused && !RendererFocused && App.RendererVisible)
                {
                    _ = App.SetEditorRenderingAsync(false);
                    Debug.Log("Set rendering false");
                }
                else if ((EditorFocused || RendererFocused) && !App.RendererVisible)
                {
                    EnvironmentWindow? win = EnvironmentWindow.GetFirstActiveWindow();
                    if (win != null && win.IsLoaded)
                    {
                        initializeTimer = 60;
                        _ = App.SetEditorRenderingAsync(true);
                        Debug.Log("Set rendering true");
                    }
                }
            });
            updatingFocus = false;
        }

        public override void Render()
        {
            bool forceGrab = false;
            forceGrabWindowTimer++; // Force window grab every 20 rendered frames.
            if (forceGrabWindowTimer > 20)
            {
                forceGrab = true;
                forceGrabWindowTimer = 0;
            }
            Dispatcher.UIThread.Post(() => UpdateRenderer(forceGrab), DispatcherPriority.Render);
        }

        /// <summary>
        /// Sets whether the Silk.NET physical render window is enabled.
        /// </summary>
        /// <param name="visible">Whether the OpenGL renderer is visible</param>
        public static void SetVisible(bool visible)
        {
            if (visible != App.RendererVisible)
                _ = App.SetEditorRenderingAsync(visible);
        }

        /// <summary>
        /// Updates the render position, size, and visibility.
        /// </summary>
        private void UpdateRenderer(bool forceGrab)
        {
            try
            {
                if (App.Renderer == null || App.Renderer.RendererWindow == null) return;

                EnvironmentWindow? win = EnvironmentWindow.GetFirstActiveWindow();
                if (win != null && win.IsLoaded && App.RendererVisible)
                {
                    // Check frame is active
                    if (win.renderVisualizerFrame == null || App.Renderer?.RendererWindow == null ||
                        win.renderVisualizerFrame.Bounds.Width <= 0 || win.renderVisualizerFrame.Bounds.Height <= 0)
                        return;

                    PixelPoint screenPoint = win.renderVisualizerFrame.PointToScreen(new Point(0, 0));
                    Size size = win.renderVisualizerFrame.Bounds.Size;

                    // Check if frame changed shape
                    if (!forceGrab && size.Width == prevWidth && size.Height == prevHeight
                        && prevX == screenPoint.X && prevY == screenPoint.Y) return;

                    // Update bounds
                    prevWidth = size.Width;
                    prevHeight = size.Height;
                    prevX = screenPoint.X;
                    prevY = screenPoint.Y;

                    App.Renderer.RendererWindow.Position = new Silk.NET.Maths.Vector2D<int>(screenPoint.X, screenPoint.Y);
                    App.Renderer.RendererWindow.Size = new Silk.NET.Maths.Vector2D<int>((int)size.Width, (int)size.Height);

                    // Update window text
                    win.widthHeightText.Text = $"(Width {(int)size.Width}px,  Height {(int)size.Height}px,  FPS {TimeSystem.FPS})";
                }
                else if (App.RendererVisible) SetVisible(false);
            }
            catch (Exception ex)
            {
                Debug.Error("Failed to update renderer window", ex);
            }
        }
    }
}
