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
using Material.Icons;
using System;

namespace DivisionEngine.Editor.Tasks
{
    /// <summary>
    /// Represents a task in the editor.
    /// </summary>
    /// <remarks>
    /// Creates a new editor task object.
    /// </remarks>
    /// <param name="name">Name of task</param>
    /// <param name="description">Description of task</param>
    /// <param name="initProgress">Initial progress of task from 0 - 1</param>
    /// <param name="icon">Icon to represent task</param>
    internal class EditorTask(string name, string description, float initProgress = 0f, MaterialIconKind icon = MaterialIconKind.TaskAuto)
    {
        /// <summary>
        /// Name of this task.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// ID of this task.
        /// </summary>
        public Guid Id { get; set; } = Guid.CreateVersion7();

        /// <summary>
        /// Description of this task.
        /// </summary>
        public string Description { get; set; } = description;

        /// <summary>
        /// Progress of this task.
        /// </summary>
        public float Progress { get; set; } = initProgress;

        /// <summary>
        /// Icon representing this task.
        /// </summary>
        public MaterialIconKind Icon { get; set; } = icon;

        /// <summary>
        /// Called when the task is completed.
        /// </summary>
        public Action? OnComplete { get; set; }

        /// <summary>
        /// If the task has been completed.
        /// </summary>
        public bool IsComplete => Progress >= 1.0f;
    }
}
