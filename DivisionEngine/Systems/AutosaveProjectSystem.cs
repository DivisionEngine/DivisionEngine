using DivisionEngine.Editor.Settings;
using DivisionEngine.Projects;

namespace DivisionEngine.Editor.Systems
{
    /// <summary>
    /// Auto-saves projects if EditorSettings.AutoSave is enabled.
    /// </summary>
    internal class AutosaveProjectSystem : SystemBase
    {
        private int saveProjectTimer;

        public override void Awake()
        {
            // Default save timer
            // 60 fps = save every 2 seconds, 30 fps = save every 4 seconds, etc.
            saveProjectTimer = 120;
        }

        public override void Update()
        {
            if (EditorSettings.Instance.AutoSave && ProjectManager.IsCurrentLoaded)
            {
                saveProjectTimer--;
                if (saveProjectTimer < 1)
                {
                    ProjectManager.SaveCurrentProject();
                    saveProjectTimer = EditorSettings.Instance.AutoSaveInterval;
                }
            }
        }

        public override void Unload()
        {
            if (EditorSettings.Instance.AutoSave) ProjectManager.SaveCurrentProject();
        }
    }
}
