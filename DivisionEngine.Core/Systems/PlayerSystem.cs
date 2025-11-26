using DivisionEngine.Components;
using DivisionEngine.Input;
using System.Numerics;

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
            //Debug.Info($"Transform forward: ({transform.Forward.X}, {transform.Forward.Y}, {transform.Forward.Z})");
        }

        private void HandleMouseLook(Transform transform, Player player)
        {
            if (InputSystem.IsMousePressed(MouseCode.Right))
            {
                Vector2 mouseDelta = InputSystem.MouseUVDelta;
                if (mouseDelta.X == 0f && mouseDelta.Y == 0f) return;

                float yaw = mouseDelta.X * player.mouseSensitivity;
                float pitch = -mouseDelta.Y * player.mouseSensitivity;

                float4 currentRot = transform.rotation;
                float4 yawRot = 
            }
        }
    }
}
