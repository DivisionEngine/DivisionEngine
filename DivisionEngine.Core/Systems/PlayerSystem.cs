//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components;
using DivisionEngine.Input;
using DivisionEngine.MathLib;
using DivisionEngine.Rendering;
using DivisionEngine.Settings;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Handles basic 3D player movement and controls.
    /// </summary>
    public class PlayerSystem : SystemBase
    {
        /// <summary>
        /// Tracks the window focus of the render window to make sure focused before updating player.
        /// </summary>
        private static bool WindowFocused { get; set; } = false;

        public override void Awake()
        {
            RenderPipeline.RenderWindowFocusd += (f) => WindowFocused = f;
        }

        public override void Update()
        {
            if (WindowFocused)
            {
                foreach (var (_, transform, player) in W.QueryData<Transform, Player>())
                {
                    HandleKeyboardMovement(transform, player);
                    HandleMouseLook(transform, player);
                }
            }
        }

        private static void HandleKeyboardMovement(Transform transform, Player player)
        {
            float deltaTime = TimeSystem.DeltaTimeF;
            float speed = player.movementSpeed * deltaTime;

            if (InputSystem.IsPressed(KeyCode.ShiftLeft) || InputSystem.IsPressed(KeyCode.ShiftRight))
                speed *= player.sprintMultiplier;

            float3 position = transform.position;
            float3 forward = transform.Forward;
            float3 right = transform.Right;
            float3 up = transform.Up;

            float3 movement = new float3(0f, 0f, 0f);
            if (InputSystem.IsPressed(KeyCode.W) || InputSystem.IsPressed(KeyCode.ArrowUp))
                movement = movement.Add(forward.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.A) || InputSystem.IsPressed(KeyCode.ArrowLeft))
                movement = movement.Subtract(right.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.S) || InputSystem.IsPressed(KeyCode.ArrowDown))
                movement = movement.Subtract(forward.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.D) || InputSystem.IsPressed(KeyCode.ArrowRight))
                movement = movement.Add(right.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.Q) || InputSystem.IsPressed(KeyCode.PageDown))
                movement = movement.Subtract(up.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.E) || InputSystem.IsPressed(KeyCode.PageUp))
                movement = movement.Add(up.Multiply(speed));

            player.movementSpeed += InputSystem.ScrollDelta.Y * player.movementSpeed * 0.05f;
            position = position.Add(movement);
            transform.position = position;
        }

        private static void HandleMouseLook(Transform transform, Player player)
        {
            if (InputSystem.IsMousePressed(MouseCode.Right))
            {
                float2 mouseDelta = InputSystem.MouseUVDelta;
                if (mouseDelta.X == 0f && mouseDelta.Y == 0f) return;

                EngineSettings settings = EngineSettings.Instance;
                float yaw = mouseDelta.X * player.mouseSensitivity * settings.MouseSensitivity;
                float pitch = -mouseDelta.Y * player.mouseSensitivity * settings.MouseSensitivity;

                float4 currentRot = transform.rotation;
                float4 yawRot = Quaternion.CreateFromAxisAngle(new float3(0, 1, 0), yaw);
                float4 pitchRot = Quaternion.CreateFromAxisAngle(new float3(1, 0, 0), pitch);

                // Apply yaw first, then pitch, order matters!
                float4 newRot = Quaternion.Multiply(currentRot, yawRot);
                newRot = Quaternion.Multiply(pitchRot, newRot);
                transform.rotation = newRot.Normalize();
            }
        }
    }
}
