//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System;
using System.Collections.Generic;
using System.Linq;

namespace DivisionEngine.Editor.Undo
{
    /// <summary>
    /// Handles state management for undo/redo operations.
    /// </summary>
    public static class UndoManager
    {
        private const int MaxUndoCount = 100;
        private static Stack<IUndoCommand> undoStack = new();
        private static readonly Stack<IUndoCommand> redoStack = new();

        public static event Action? UndoStackChanged;

        public static bool CanUndo => undoStack.Count > 0;
        public static bool CanRedo => redoStack.Count > 0;
        public static bool IsExecuting { get; private set; } = false;

        /// <summary>
        /// Executes an undo/redo command.
        /// </summary>
        /// <param name="command">Command to execute</param>
        public static void Execute(IUndoCommand command)
        {
            if (IsExecuting)
            {
                // Called from within another command – just execute without recording
                command.Do();
                return;
            }

            IsExecuting = true;
            try
            {
                command.Do();
                undoStack.Push(command);
                redoStack.Clear();
                if (undoStack.Count > MaxUndoCount) // remove oldest (bottom)
                    undoStack = new Stack<IUndoCommand>(undoStack.Reverse().Take(MaxUndoCount).Reverse());
                UndoStackChanged?.Invoke();
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public static void Undo()
        {
            if (!CanUndo) return;
            IsExecuting = true;
            try
            {
                IUndoCommand command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
                UndoStackChanged?.Invoke();
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public static void Redo()
        {
            if (!CanRedo) return;
            IsExecuting = true;
            try
            {
                IUndoCommand command = redoStack.Pop();
                command.Do();
                undoStack.Push(command);
                UndoStackChanged?.Invoke();
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public static void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            UndoStackChanged?.Invoke();
        }
    }
}
