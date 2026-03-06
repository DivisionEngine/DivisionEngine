using DivisionEngine.Settings;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DivisionEngine.Editor.Settings
{
    /// <summary>
    /// Holds all the Division editor settings.
    /// </summary>
    internal class EditorSettings : BaseSettings
    {
        private static EditorSettings? instance;

        /// <summary>
        /// Gets the singleton instance of the editor settings, loading it from disk if it does not already exist.
        /// </summary>
        /// <remarks>This property ensures that only one instance of the editor settings is used
        /// throughout the application. The instance is initialized lazily and is loaded from persistent storage when
        /// first accessed.</remarks>
        [JsonIgnore]
        public static EditorSettings Instance
        {
            get
            {
                // Try to load from disk, or create new if doesn't exist
                instance ??= SettingsManager.GetSettings<EditorSettings>();
                return instance;
            }
        }

        /// <summary>
        /// Editor settings file identifier.
        /// </summary>
        public override string ID => "EditorSettings";

        // Load will happen through SettingsManager
        public EditorSettings() { }

        protected override void InitializeDefaults()
        {
            AutoSave = true;
            AutoSaveInterval = 120;
            RecentProjects = [];
            MaxRecentProjects = 10;
            MainWindowWidth = 1280;
            MainWindowHeight = 720;
            MainWindowMaximized = false;
            ShowGridLines = true;
            Theme = "Dark";
            FontSize = 11.0;
            ShowWelcomeScreen = true;
        }

        [JsonIgnore]
        public bool AutoSave
        {
            get => Get(nameof(AutoSave), true);
            set => Set(nameof(AutoSave), value);
        }

        [JsonIgnore]
        public int AutoSaveInterval
        {
            get => Get(nameof(AutoSaveInterval), 120);
            set => Set(nameof(AutoSaveInterval), value);
        }

        [JsonIgnore]
        public List<string> RecentProjects
        {
            get => Get(nameof(RecentProjects), new List<string>());
            set => Set(nameof(RecentProjects), value);
        }

        [JsonIgnore]
        public int MaxRecentProjects
        {
            get => Get(nameof(MaxRecentProjects), 10);
            set => Set(nameof(MaxRecentProjects), value);
        }

        [JsonIgnore]
        public double MainWindowWidth
        {
            get => Get(nameof(MainWindowWidth), 1280.0);
            set => Set(nameof(MainWindowWidth), value);
        }

        [JsonIgnore]
        public double MainWindowHeight
        {
            get => Get(nameof(MainWindowHeight), 720.0);
            set => Set(nameof(MainWindowHeight), value);
        }

        [JsonIgnore]
        public bool MainWindowMaximized
        {
            get => Get(nameof(MainWindowMaximized), false);
            set => Set(nameof(MainWindowMaximized), value);
        }

        [JsonIgnore]
        public bool ShowGridLines
        {
            get => Get(nameof(ShowGridLines), true);
            set => Set(nameof(ShowGridLines), value);
        }

        [JsonIgnore]
        public string Theme
        {
            get => Get(nameof(Theme), "Dark");
            set => Set(nameof(Theme), value);
        }

        [JsonIgnore]
        public double FontSize
        {
            get => Get(nameof(FontSize), 11.0);
            set => Set(nameof(FontSize), value);
        }

        [JsonIgnore]
        public bool ShowWelcomeScreen
        {
            get => Get(nameof(ShowWelcomeScreen), true);
            set => Set(nameof(ShowWelcomeScreen), value);
        }

        // Helper methods
        public void AddRecentProject(string path)
        {
            List<string> recent = RecentProjects;
            recent.Remove(path);
            recent.Insert(0, path);

            if (recent.Count > MaxRecentProjects)
                recent.RemoveRange(MaxRecentProjects, recent.Count - MaxRecentProjects);
            RecentProjects = recent;
        }

        /// <summary>
        /// Initializes the editor settings and enforces minimum and maximum values for configuration properties.
        /// </summary>
        /// <remarks>This method ensures that the FontSize, MaxRecentProjects, and AutoSaveInterval
        /// properties remain within their valid ranges. It is typically called during the loading process to guarantee
        /// that settings are consistent and within supported limits.</remarks>
        public override void OnLoad()
        {
            if (FontSize < 8) FontSize = 8;
            if (FontSize > 24) FontSize = 24;
            if (MaxRecentProjects < 5) MaxRecentProjects = 5;
            if (AutoSaveInterval < 30) AutoSaveInterval = 30;
        }
    }
}
