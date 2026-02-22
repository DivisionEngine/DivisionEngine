//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
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
