//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects
{
    /// <summary>
    /// Represents a project in the Division Engine, used for serializing project data.
    /// </summary>
    public class DivisionProject
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime LastSaved { get; set; }

        private EditorLayoutData? editorLayout;

        public EditorLayoutData EditorLayout
        {
            get => editorLayout ??= new EditorLayoutData();
            set => editorLayout = value;
        }

        public DivisionProject()
        {
            Name = string.Empty;
            Version = string.Empty;
            editorLayout = new EditorLayoutData();
        }

        public DivisionProject(string name)
        {
            Name = name;
            Version = "1.0.0";
            LastSaved = DateTime.Now;
            editorLayout = new EditorLayoutData();
        }
    }

    /// <summary>
    /// Stores editor layout configuration for a project.
    /// </summary>
    public class EditorLayoutData
    {
        // Grid splitter positions
        public double LeftPanelWidth { get; set; } = 200;
        public double RightPanelWidth { get; set; } = 300;
        public double BottomPanelHeight { get; set; } = 250;
        public double CenterTopHeight { get; set; } = -1; // -1 means automatic/not set

        // Selected tabs per panel
        public string SelectedLeftTab { get; set; } = "WorldWindow";
        public string SelectedRightTab { get; set; } = "PropertiesWindow";
        public string SelectedCenterTab { get; set; } = "EnvironmentWindow";
        public string SelectedBottomTab { get; set; } = "AssetsWindow";

        // Use comma-separated strings
        public string LeftTabs { get; set; } = "WorldWindow";
        public string RightTabs { get; set; } = "PropertiesWindow,SettingsWindow";
        public string CenterTabs { get; set; } = "EnvironmentWindow";
        public string BottomTabs { get; set; } = "AssetsWindow,ConsoleWindow,DeveloperWindow";
    }
}
