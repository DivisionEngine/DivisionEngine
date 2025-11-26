using DivisionEngine.Components;
using DivisionEngine.Input;
using DivisionEngine.MathLib;

namespace DivisionEngine.Systems
{
    public class PlayerSystem : SystemBase
    {
        public override void Update()
        {
            foreach (var (_, transform, player) in W.QueryData<Transform, Player>())
                HandlePlayerInput(transform, player);
        }

        private void HandlePlayerInput(Transform transform, Player player)
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
            float3 forward = transform.Forward.Normalize();
            float3 right = transform.Right.Normalize();
            float3 up = transform.Up.Normalize();

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

                Debug.Info($"yaw {yaw}, pitch {pitch}");

                float4 currentRot = transform.rotation;
                float4 yawRot = Quaternion.CreateFromAxisAngle(new float3(0, 1, 0), yaw);
                float4 pitchRot = Quaternion.CreateFromAxisAngle(transform.Right, pitch);

                float4 newRot = Quaternion.Multiply(pitchRot, Quaternion.Multiply(currentRot, yawRot));
                transform.rotation = newRot.Normalize();
            }
        }
    }
}
