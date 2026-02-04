using Material.Icons;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DivisionEngine.Editor.Tasks
{
    /// <summary>
    /// Used for accessing the editor task system.
    /// </summary>
    internal static class EditorTaskManager
    {
        private static readonly ConcurrentDictionary<Guid, EditorTask> tasks = new();

        /// <summary>
        /// Called when tasks have been updated.
        /// </summary>
        public static event Action? TasksChanged;

        public static EditorTask Create(string name, string description, float initProgress = 0f, MaterialIconKind icon = MaterialIconKind.TaskAuto)
        {
            EditorTask task = new EditorTask(name, description, initProgress, icon);
            tasks[task.Id] = task;
            TasksChanged?.Invoke();
            return task;
        }

        public static void Update(Guid id, float progress)
        {
            if (tasks.TryGetValue(id, out EditorTask? task))
            {
                task.Progress = progress;
                TasksChanged?.Invoke();
            }
        }

        public static void Complete(Guid id) => Update(id, 1);
        public static IEnumerable<EditorTask> GetAll() => tasks.Values;
        public static void Remove(Guid id)
        {
            if (tasks.TryRemove(id, out _))
            {
                TasksChanged?.Invoke(); // Make sure this line exists!
            }
        }
    }
}
