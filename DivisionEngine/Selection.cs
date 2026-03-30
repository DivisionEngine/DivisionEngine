//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System;

namespace DivisionEngine
{
    /// <summary>
    /// Stores the current selection in the editor.
    /// </summary>
    public static class Selection
    {
        /// <summary>
        /// Represents the type of object selected.
        /// </summary>
        public enum SelectionType
        {
            None, Entity, Asset, Other
        }

        public static Action<object?>? OnSelectionChanged { get; set; }
        public static object? SelectedObj { get; private set; }
        public static SelectionType SelectedType { get; private set; } = SelectionType.None;

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public static void Clear()
        {
            SelectedObj = null;
            SelectedType = SelectionType.None;
            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// Selects the uint entity ID.
        /// </summary>
        /// <param name="entityID">ID of an entity</param>
        public static void SelectEntity(uint entityID)
        {
            SelectedObj = entityID;
            SelectedType = SelectionType.Entity;
            OnSelectionChanged?.Invoke(entityID);
        }

        /// <summary>
        /// Selects the GUID of an asset.
        /// </summary>
        /// <param name="assetID">Asset GUID</param>
        public static void SelectAsset(string assetID)
        {
            SelectedObj = assetID;
            SelectedType = SelectionType.Asset;
            OnSelectionChanged?.Invoke(assetID);
        }

        /// <summary>
        /// Selects an object.
        /// </summary>
        /// <param name="selection">Selected object</param>
        public static void SelectObject(object selection)
        {
            SelectedObj = selection;
            SelectedType = SelectionType.Asset;
            OnSelectionChanged?.Invoke(selection);
        }
    }
}
