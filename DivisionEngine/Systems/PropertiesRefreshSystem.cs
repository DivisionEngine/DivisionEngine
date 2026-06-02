//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System;
using System.Collections.Generic;

namespace DivisionEngine.Editor.Systems
{
    public class PropertiesRefreshSystem : SystemBase
    {
        public override int Priority => -100;

        private static uint lastSelectedEntity = uint.MaxValue;
        private static HashSet<Type> componentsToRefresh = [];
        private static int framesSinceLastRefresh = 0;
        private static bool needsRefresh = false;

        public override void EditorUpdate()
        {
            if (lastSelectedEntity == uint.MaxValue) return;
            if (!needsRefresh && componentsToRefresh.Count == 0) return;

            framesSinceLastRefresh++;

            // Refresh after 2 frames (allows multiple changes to batch together)
            if (framesSinceLastRefresh >= 2)
            {
                framesSinceLastRefresh = 0;
                needsRefresh = false;

                // Make a copy to avoid modification during iteration
                var refreshesToProcess = new HashSet<Type>(componentsToRefresh);

                foreach (var compType in refreshesToProcess)
                {
                    foreach (var window in PropertiesWindow.GetCurrentWindows())
                    {
                        window?.RefreshComponent(compType);
                    }
                }

                // Don't clear! Keep the components that still need refreshing for next frame
                // Instead, we'll rely on new OnFieldChanged calls to keep them in the set
            }
        }

        public static void OnEntitySelected(uint entityId)
        {
            lastSelectedEntity = entityId;
            componentsToRefresh.Clear();
            needsRefresh = false;
            framesSinceLastRefresh = 0;
        }

        public static void OnFieldChanged(uint entityId, string componentType, string fieldName)
        {
            if (entityId != lastSelectedEntity) return;

            // Find the actual Type object
            Type? compType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                compType = assembly.GetType(componentType);
                if (compType != null) break;
            }

            if (compType == null) return;

            componentsToRefresh.Add(compType);
            needsRefresh = true;
            framesSinceLastRefresh = 0; // Reset frame counter
        }

        public static void ClearSelection()
        {
            lastSelectedEntity = uint.MaxValue;
            componentsToRefresh.Clear();
            needsRefresh = false;
            framesSinceLastRefresh = 0;
        }
    }
}
