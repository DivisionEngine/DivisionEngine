//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components;
using DivisionEngine.Editor.Settings;
using DivisionEngine.Input;
using DivisionEngine.MathLib;
using DivisionEngine.Rendering;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Handles editor transform gizmo interaction.
    /// </summary>
    public class HandleInteractionSystem : SystemBase
    {
        public override int Priority => -10;

        private static bool isDragging = false;
        private static uint selectedHandle = 0;
        private static uint draggedEntity = uint.MaxValue;
        private static float3 currentPosition;
        private static float2 lastMousePos;
        private static float3 cameraPosition;
        private static float3 cameraRight;
        private static float3 cameraUp;
        private static float distanceToCamera;

        public override void AppStart()
        {
            Selection.OnSelectionChanged += (t) =>
            {
                if (Selection.SelectedType == SelectionType.Entity && t is uint entity)
                {
                    draggedEntity = entity;
                    Debug.Info($"HandleInteraction: Entity selected {t}");
                }
                else
                {
                    draggedEntity = uint.MaxValue;
                    RenderPipeline.Instance?.HideHandles();
                    Debug.Info("HandleInteraction: No entity selected");
                }
            };
            EngineCore.PlayModeChanged += EngineCore_PlayModeChanged;
        }

        private void EngineCore_PlayModeChanged(bool inPlayMode)
        {
            if (inPlayMode) RenderPipeline.Instance?.HideHandles();
        }

        public override void EditorUpdate()
        {
            if (RenderPipeline.Instance == null || EngineCore.IsInPlayMode) return;

            // Always update handle position to match selected entity's transform
            if (draggedEntity != uint.MaxValue && !isDragging)
            {
                Transform? transform = W.GetComponent<Transform>(draggedEntity);
                if (transform != null) RenderPipeline.Instance?.ShowHandles(transform.position, EditorSettings.Instance!.EditorHandleScale);
            }

            float2 mousePos = InputSystem.MousePosition;
            int pixelX = (int)mousePos.X;
            int pixelY = (int)mousePos.Y;
            bool mouseDown = InputSystem.IsMousePressed(MouseCode.Left);
            RenderPipeline.Instance!.UpdateHoveredHandle((int)mousePos.X, (int)mousePos.Y);

            // Start dragging
            if (mouseDown && !isDragging)
            {
                uint handleId = RenderPipeline.Instance.GetHandleAtPosition(pixelX, pixelY);
                if (handleId > 0 && handleId <= 3 && draggedEntity != uint.MaxValue)
                {
                    isDragging = true;
                    selectedHandle = handleId;
                    lastMousePos = mousePos;

                    // Store current position
                    var entityTransform = W.GetComponent<Transform>(draggedEntity);
                    if (entityTransform != null)
                    {
                        currentPosition = entityTransform.position;
                        Debug.Info($"HandleInteraction: Started dragging handle {selectedHandle}, position {currentPosition}");
                    }
                    else
                    {
                        Debug.Error($"HandleInteraction: No Transform component on entity {draggedEntity}");
                        isDragging = false;
                        return;
                    }

                    // Get camera data
                    Transform? camTransform = GetMainCamera();
                    if (camTransform != null)
                    {
                        cameraPosition = camTransform.position;
                        cameraRight = camTransform.Right;
                        cameraUp = camTransform.Up;

                        distanceToCamera = Math.Sqrt(
                            (cameraPosition.X - currentPosition.X) * (cameraPosition.X - currentPosition.X) +
                            (cameraPosition.Y - currentPosition.Y) * (cameraPosition.Y - currentPosition.Y) +
                            (cameraPosition.Z - currentPosition.Z) * (cameraPosition.Z - currentPosition.Z)
                        );
                    }
                    else
                    {
                        Debug.Error("HandleInteraction: No camera found");
                        isDragging = false;
                    }
                }
            }

            // Dragging
            if (isDragging && mouseDown)
            {
                // Calculate delta from LAST frame
                float2 delta = new float2(
                    mousePos.X - lastMousePos.X,
                    -(mousePos.Y - lastMousePos.Y)
                );

                if (delta.X != 0 || delta.Y != 0)
                {
                    // Convert screen delta to world movement on the selected axis
                    float3 movement = GetAxisMovement(delta);

                    // Scale by distance to camera for natural feel
                    float distanceScale = distanceToCamera * 0.002f;
                    movement = new float3(
                        movement.X * distanceScale,
                        movement.Y * distanceScale,
                        movement.Z * distanceScale
                    );

                    // Apply movement
                    Transform? transform = W.GetComponent<Transform>(draggedEntity);
                    if (transform != null)
                    {
                        currentPosition = new float3(
                            currentPosition.X + movement.X,
                            currentPosition.Y + movement.Y,
                            currentPosition.Z + movement.Z
                        );
                        transform.position = currentPosition;
                        RenderPipeline.Instance?.ShowHandles(transform.position, EditorSettings.Instance!.EditorHandleScale);

                        // Update distance as we move
                        distanceToCamera = Math.Sqrt(
                            (cameraPosition.X - currentPosition.X) * (cameraPosition.X - currentPosition.X) +
                            (cameraPosition.Y - currentPosition.Y) * (cameraPosition.Y - currentPosition.Y) +
                            (cameraPosition.Z - currentPosition.Z) * (cameraPosition.Z - currentPosition.Z)
                        );
                    }
                }

                lastMousePos = mousePos;
            }

            // Stop dragging
            if (!mouseDown && isDragging)
            {
                Debug.Info($"HandleInteraction: Stopped dragging handle {selectedHandle}");
                isDragging = false;
                selectedHandle = 0;
            }
        }

        private static float3 GetAxisMovement(float2 delta)
        {
            // Get the world axis direction for the selected handle
            float3 axisDirection = selectedHandle switch
            {
                1 => new float3(1, 0, 0), // X axis
                2 => new float3(0, 1, 0), // Y axis
                3 => new float3(0, 0, 1), // Z axis
                _ => float3.Zero
            };

            // Project the axis direction onto camera's screen plane
            // 2D vector representing how the axis appears on screen
            float2 screenAxis = new float2(
                Vector.Dot(axisDirection, cameraRight),
                Vector.Dot(axisDirection, cameraUp)
            );

            // Normalize the screen axis to get direction
            float screenAxisLength = Math.Sqrt(screenAxis.X * screenAxis.X + screenAxis.Y * screenAxis.Y);
            if (screenAxisLength > 0.001f)
                screenAxis = new float2(screenAxis.X / screenAxisLength, screenAxis.Y / screenAxisLength);
            else
                // Axis is perpendicular to view direction (looking straight down the axis)
                // Fall back to using camera right/up
                screenAxis = selectedHandle == 1 ? new float2(1, 0) : new float2(0, 1);

            // Project mouse delta onto the screen-space axis direction
            float projection = delta.X * screenAxis.X + delta.Y * screenAxis.Y;

            // Convert back to world movement
            // The movement is positive when dragging along the positive screen direction
            return new float3(
                axisDirection.X * projection,
                axisDirection.Y * projection,
                axisDirection.Z * projection
            );
        }

        private static Transform? GetMainCamera()
        {
            foreach (var (_, transform, camera) in W.QueryData<Transform, Camera>())
                if (camera.isActive)
                    return transform;
            return null;
        }
    }
}
