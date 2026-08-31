//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System;
using System.Reflection;

namespace DivisionEngine.Editor.Undo
{
    public class ModifyFieldCommand(uint entityId, Type componentType, string fieldName, object? oldValue, object? newValue) : IUndoCommand
    {
        public string Description => $"Modify field {fieldName}_{entityId}";

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
            World? w = WorldManager.CurrentWorld;
            if (w == null || !w.EntityExists(entityId)) return;
            IComponent? comp = w.GetComponent(entityId, componentType);
            if (comp == null) return;
            FieldInfo? field = componentType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            field?.SetValue(comp, value);
        }
    }
}
