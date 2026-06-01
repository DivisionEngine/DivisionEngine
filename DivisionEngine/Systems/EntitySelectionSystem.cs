//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Components;
using DivisionEngine.Editor.Settings;
using DivisionEngine.Input;
using DivisionEngine.Rendering;
using System;

namespace DivisionEngine.Editor.Systems
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

        public override void EditorUpdate()
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

                // Check if clicking on a handle
                uint handleAtClick = RenderPipeline.Instance.GetHandleAtPosition(pixelX, pixelY);
                if (handleAtClick > 0)
                {
                    Debug.Info($"EntitySelectionSystem: Click on handle {handleAtClick}, blocking selection");
                    selectEnabled = false;
                    return;
                }

                // Only proceed with entity selection if NOT clicking on a handle
                uint entitySelected = RenderPipeline.Instance.ObjectIDs[pixelX + (height - pixelY) * width].Y;
                if (entitySelected != uint.MaxValue)
                {
                    if (W.HasComponent<Transform>(entitySelected))
                    {
                        Transform? transform = W.GetComponent<Transform>(entitySelected);
                        RenderPipeline.Instance?.ShowHandles(transform!.position, EditorSettings.Instance!.EditorHandleScale);
                    }
                    OnEntitySelected?.Invoke(entitySelected);
                }
                else
                {
                    RenderPipeline.Instance?.HideHandles();
                    OnNoEntityFound?.Invoke();
                }
                selectEnabled = false;
            }
        }
    }
}
