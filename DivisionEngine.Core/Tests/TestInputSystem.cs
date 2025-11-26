using DivisionEngine.Input;
using static DivisionEngine.Debug;

namespace DivisionEngine.Tests
{
    /// <summary>
    /// Used for testing the input system
    /// </summary>
    internal class TestInputSystem : SystemBase
    {
        public override void Update()
        {
            //Info($"K key is pressed: {InputSystem.IsPressed(KeyCode.K)}");
            //Info($"Left mouse is pressed: {InputSystem.IsMousePressed(MouseCode.Left)}");
            Info($"Mouse position: ({InputSystem.MouseUVDelta.X}, {InputSystem.MouseUVDelta.Y})");
        }
    }
}
