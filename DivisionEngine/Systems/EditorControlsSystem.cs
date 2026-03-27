using DivisionEngine.Input;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Used to handle editor controls and shortcuts.
    /// </summary>
    internal class EditorControlsSystem : SystemBase
    {
        public override void Update()
        {
            if (InputSystem.IsPressed(KeyCode.Delete))
            {
                
            }
        }
    }
}
