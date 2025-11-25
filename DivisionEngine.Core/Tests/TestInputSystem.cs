using DivisionEngine.Input;

namespace DivisionEngine.Tests
{
    /// <summary>
    /// Used for testing the input system
    /// </summary>
    internal class TestInputSystem : SystemBase
    {
        public override void Update()
        {
            //Debug.Info($"K key is pressed: {InputSystem.IsPressed(KeyCode.K)}");
            //Debug.Info($"Left mouse is pressed: {InputSystem.IsMousePressed(MouseCode.Left)}");
        }
    }
}
