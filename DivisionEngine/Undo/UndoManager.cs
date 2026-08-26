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
    public static class UndoManager
    {
        private const int MaxUndoCount = 100;
        private static Stack<IUndoCommand> undoStack = new();
        private static readonly Stack<IUndoCommand> redoStack = new();
        private static bool isExecuting = false; // prevent recursive recording

        public static event Action? UndoStackChanged;

        public static bool CanUndo => undoStack.Count > 0;
        public static bool CanRedo => redoStack.Count > 0;

        public static void Execute(IUndoCommand command)
        {
            if (isExecuting)
            {
                // Called from within another command – just execute without recording
                command.Do();
                return;
            }

            isExecuting = true;
            try
            {
                command.Do();
                undoStack.Push(command);
                redoStack.Clear();
                if (undoStack.Count > MaxUndoCount)
                    // remove oldest (bottom)
                    undoStack = new Stack<IUndoCommand>(
                        undoStack.Reverse().Take(MaxUndoCount).Reverse());
                UndoStackChanged?.Invoke();
            }
            finally
            {
                isExecuting = false;
            }
        }

        public static void Undo()
        {
            if (!CanUndo) return;
            isExecuting = true;
            try
            {
                var command = undoStack.Pop();
                command.Undo();
                redoStack.Push(command);
                UndoStackChanged?.Invoke();
            }
            finally
            {
                isExecuting = false;
            }
        }

        public static void Redo()
        {
            if (!CanRedo) return;
            isExecuting = true;
            try
            {
                var command = redoStack.Pop();
                command.Do();
                undoStack.Push(command);
                UndoStackChanged?.Invoke();
            }
            finally
            {
                isExecuting = false;
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
