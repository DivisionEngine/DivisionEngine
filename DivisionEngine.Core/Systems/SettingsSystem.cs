using DivisionEngine.Settings;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Initializes engine settings.
    /// </summary>
    internal class SettingsSystem : SystemBase
    {
        private int saveTimer;
        private const int SAVE_INTERVAL = 900; // Save every 900 frames (at 60 fps about 15 seconds, 30fps, 30s)
        private bool hasChanges;

        public override void Awake()
        {
            // Ensure engine settings are loaded
            _ = EngineSettings.Instance;
        }

        public override void Update()
        {
            if (!hasChanges) return;

            saveTimer += 1;
            if (saveTimer >= SAVE_INTERVAL)
            {
                SettingsManager.SaveAll();
                saveTimer = 0;
                hasChanges = false;
            }
        }

        public override void Unload() => SettingsManager.SaveAll();

        /// <summary>
        /// Call this whenever a setting changes.
        /// </summary>
        public void MarkDirty() => hasChanges = true;
    }
}
