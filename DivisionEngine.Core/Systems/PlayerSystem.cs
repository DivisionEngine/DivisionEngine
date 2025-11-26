using DivisionEngine.Components;
using DivisionEngine.Input;
using DivisionEngine.MathLib;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Handles basic 3D player movement and controls
    /// </summary>
    public class PlayerSystem : SystemBase
    {
        public override void Update()
        {
            //TestRotation();

            foreach (var (_, transform, player) in W.QueryData<Transform, Player>())
                HandlePlayerInput(transform, player);
        }

        private static void HandlePlayerInput(Transform transform, Player player)
        {
            HandleKeyboardMovement(transform, player);
            HandleMouseLook(transform, player);
        }

        private static void HandleKeyboardMovement(Transform transform, Player player)
        {
            float deltaTime = TimeSystem.DeltaTimeF;
            float speed = player.movementSpeed * deltaTime;

            if (InputSystem.IsPressed(KeyCode.ShiftLeft)) speed *= player.sprintMultiplier;

            float3 position = transform.position;
            float3 forward = transform.Forward;
            float3 right = transform.Right;
            float3 up = transform.Up;

            float3 movement = new float3(0f, 0f, 0f);
            if (InputSystem.IsPressed(KeyCode.W)) movement = movement.Add(forward.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.A)) movement = movement.Subtract(right.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.S)) movement = movement.Subtract(forward.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.D)) movement = movement.Add(right.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.Q)) movement = movement.Subtract(up.Multiply(speed));
            if (InputSystem.IsPressed(KeyCode.E)) movement = movement.Add(up.Multiply(speed));

            position = position.Add(movement);
            transform.position = position;
        }

        // This needs to be reworked and fixed, turning to black screen when rotated
        private static void HandleMouseLook(Transform transform, Player player)
        {
            if (InputSystem.IsMousePressed(MouseCode.Right))
            {
                float2 mouseDelta = InputSystem.MouseUVDelta;
                if (mouseDelta.X == 0f && mouseDelta.Y == 0f) return;

                float yaw = mouseDelta.X * player.mouseSensitivity;
                float pitch = -mouseDelta.Y * player.mouseSensitivity;

                float4 currentRot = transform.rotation;
                float4 yawRot = Quaternion.CreateFromAxisAngle(new float3(0, 1, 0), yaw);
                float4 pitchRot = Quaternion.CreateFromAxisAngle(transform.Right, pitch);

                float4 newRot = Quaternion.Multiply(pitchRot, Quaternion.Multiply(currentRot, yawRot));
                transform.rotation = newRot.Normalize();
            }
        }

        public static void TestRotation()
        {
            // Test identity rotation
            float4 identity = Quaternion.Identity;
            float3 forward = identity.RotateVector(new float3(0, 0, -1));
            Debug.Info($"Identity forward: {forward}"); // Should be (0, 0, -1)

            // Test 90° Yaw around Y axis
            float4 yaw90 = Quaternion.CreateFromAxisAngle(new float3(0, 1, 0), Math.PI / 2);
            float3 rotated = yaw90.RotateVector(new float3(0, 0, -1));
            Debug.Info($"90° Yaw forward: {rotated}"); // Should be (-1, 0, 0)

            // Test 90° Pitch around X axis  
            float4 pitch90 = Quaternion.CreateFromAxisAngle(new float3(1, 0, 0), Math.PI / 2);
            float3 pitched = pitch90.RotateVector(new float3(0, 0, -1));
            Debug.Info($"90° Pitch forward: {pitched}"); // Should be (0, 1, 0)
        }
    }
}
