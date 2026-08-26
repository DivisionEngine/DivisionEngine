//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System;

namespace DivisionEngine.Editor.Undo
{
    public class RemoveComponentCommand : IUndoCommand
    {
        private readonly uint entityId;
        private readonly Type componentType;
        private readonly IComponent component;

        public RemoveComponentCommand(uint entityId, Type componentType, IComponent component)
        {
            this.entityId = entityId;
            this.componentType = componentType;
            this.component = component.Clone();
        }

        public void Do()
        {
            var w = WorldManager.CurrentWorld;
            if (w != null && w.EntityExists(entityId))
                w.RemoveComponent(entityId, componentType);
        }

        public void Undo()
        {
            var w = WorldManager.CurrentWorld;
            if (w != null && w.EntityExists(entityId) && !w.HasComponent(entityId, componentType))
                w.AddComponent(entityId, component.Clone());
        }
    }
}
