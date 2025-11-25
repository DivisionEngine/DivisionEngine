using DivisionEngine.Input;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Updates the input system from the current ECS world on the fixed update loop.
    /// </summary>
    internal class InputSystemUpdateSystem : SystemBase
    {
        public override void FixedUpdate()
        {
            InputSystem.Instance!.OnFixedUpdate();
        }
    }
}
