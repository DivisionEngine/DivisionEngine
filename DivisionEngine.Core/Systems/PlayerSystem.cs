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

        private void HandleKeyboardMovement(Transform transform, Player player)
        {
            float deltaTime = TimeSystem.DeltaTimeF;
            float speed = player.movementSpeed * deltaTime;

            float3 position = transform.position;
            float3 forward = transform.Forward;
            float3 right = transform.Right;

            forward = new float3(forward.X, 0, forward.Z).Normalize();
            right = new float3(right.X, 0, right.Z).Normalize();

            float3 movement = new float3(0f, 0f, 0f);
            if (InputSystem.IsPressed(KeyCode.W)) movement = movement.Add(forward.Multiply());
        }

        private void HandleMouseLook(Transform transform, Player player)
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
