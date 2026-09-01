//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using Avalonia.Threading;
using DivisionEngine.Editor.Tasks;
using DivisionEngine.Systems;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Manages system notifications for the Division Engine editor.
    /// </summary>
    internal class NotificationsSystem : SystemBase
    {
        private static EditorTask? textureLoadingTask;

        public override void AppStart()
        {
            TextureSystem.StartedLoadingTextureData += () => Dispatcher.UIThread.Post(TextureSystem_StartedLoadingTextureData, DispatcherPriority.Normal);
            TextureSystem.UpdatedTextureData += () => Dispatcher.UIThread.Post(TextureSystem_UpdatedTextureData, DispatcherPriority.Normal);
        }
        public override void EditorUpdate()
        {
            if (textureLoadingTask != null)
            {
                EditorTaskManager.Update(textureLoadingTask.Id, TextureSystem.TextureLoadProgress);
            }
        }

        private void TextureSystem_UpdatedTextureData()
        {
            if (textureLoadingTask != null) EditorTaskManager.Remove(textureLoadingTask.Id);
            textureLoadingTask = null;
        }

        private void TextureSystem_StartedLoadingTextureData()
        {
            textureLoadingTask = EditorTaskManager.Create("Texture System", "Loading project textures", 0f, Material.Icons.MaterialIconKind.Texture);
        }
    }
}
