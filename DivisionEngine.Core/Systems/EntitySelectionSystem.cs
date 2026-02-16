using DivisionEngine.Input;
using DivisionEngine.Rendering;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Selects entities in the world when clicked.
    /// </summary>
    public class EntitySelectionSystem : SystemBase
    {

        /// <summary>
        /// Can perform entity selection.
        /// </summary>
        public static bool CanSelect { get; set; } = false;

        /// <summary>
        /// When an entity is selected, this event is called with its ID.
        /// </summary>
        public static event Action<uint>? OnEntitySelected;

        /// <summary>
        /// Called when no entity was found on select.
        /// </summary>
        public static event Action? OnNoEntityFound;

        private bool selectEnabled = false;

        public override void Update()
        {
            if (!InputSystem.IsMousePressed(MouseCode.Left)) selectEnabled = true;
            else if (CanSelect && selectEnabled && InputSystem.IsMousePressed(MouseCode.Left) &&
                RenderPipeline.Instance != null &&
                RenderPipeline.Instance.ObjectIDs != null &&
                RenderPipeline.Instance.RendererWindow != null)
            {
                int width = RenderPipeline.Instance.RendererWindow.Size.X,
                    height = RenderPipeline.Instance.RendererWindow.Size.Y,
                    pixelX = (int)InputSystem.MousePosition.X,
                    pixelY = (int)InputSystem.MousePosition.Y;
                uint entitySelected = RenderPipeline.Instance.ObjectIDs[pixelX + (height - pixelY) * width].Y;

                // Check if entity is valid
                if (entitySelected != uint.MaxValue) OnEntitySelected?.Invoke(entitySelected);
                else OnNoEntityFound?.Invoke();
                selectEnabled = false;
            }
        }

    }
}
