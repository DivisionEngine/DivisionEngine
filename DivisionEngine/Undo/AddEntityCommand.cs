//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System;
using System.Collections.Generic;

namespace DivisionEngine.Editor.Undo
{
    public class AddEntityCommand : IUndoCommand
    {
        private readonly uint entityId;
        private readonly Dictionary<Type, IComponent> components;
        private readonly string? entityName;

        public AddEntityCommand(uint entityId, Dictionary<Type, IComponent> components, string? name = null)
        {
            this.entityId = entityId;
            this.components = components;
            entityName = name;
        }

        public void Do()
        {
            var w = WorldManager.CurrentWorld;
            if (w == null) return;
            if (!w.EntityExists(entityId))
            {
                w.AddEntityWithId(entityId, entityName);
                foreach (var kv in components)
                    w.AddComponent(entityId, kv.Value.Clone());
            }
        }

        public void Undo()
        {
            var w = WorldManager.CurrentWorld;
            if (w == null) return;
            if (w.EntityExists(entityId))
                w.DestroyEntity(entityId);
        }
    }
}
