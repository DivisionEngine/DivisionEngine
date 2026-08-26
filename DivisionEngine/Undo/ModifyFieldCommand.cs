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
    public class ModifyFieldCommand : IUndoCommand
    {
        private readonly uint entityId;
        private readonly Type componentType;
        private readonly string fieldName;
        private readonly object? oldValue;
        private readonly object? newValue;

        public ModifyFieldCommand(uint entityId, Type componentType, string fieldName, object? oldValue, object? newValue)
        {
            this.entityId = entityId;
            this.componentType = componentType;
            this.fieldName = fieldName;
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public void Do()
        {
            SetField(newValue);
        }

        public void Undo()
        {
            SetField(oldValue);
        }

        private void SetField(object? value)
        {
            var w = WorldManager.CurrentWorld;
            if (w == null || !w.EntityExists(entityId)) return;
            var comp = w.GetComponent(entityId, componentType);
            if (comp == null) return;
            var field = componentType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            field?.SetValue(comp, value);
        }
    }
}
