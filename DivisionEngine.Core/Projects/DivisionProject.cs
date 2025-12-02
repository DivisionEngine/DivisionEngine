using DivisionEngine.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DivisionEngine.Editor.Projects
{
    /// <summary>
    /// Represents a project in the Division Engine.
    /// </summary>
    public class DivisionProject
    {
        public string ProjectPath { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime LastSaved { get; set; }

        public DivisionProject(string path, string name = "New Project")
        {
            LastSaved = DateTime.Now;
            Name = name;
            ProjectPath = path;
            Version = "1.0.0";
        }

        private void Load()
        {

        }

        /// <summary>
        /// Save the currently loaded project as this project
        /// </summary>
        public void Save()
        {
            // Implement saving project logic here

            if (WorldManager.CurrentWorld != null)
            {
                WorldData worldData = new WorldData("default", WorldManager.CurrentWorld!);
                string serialized = JsonSerializer.Serialize(worldData);
                Debug.Info(serialized);
            }
        }

        /// <summary>
        /// Directory setup should be:
        /// Project Folder/
        /// - project.divproj
        /// - world.json
        /// - Assets/
        ///     - example.png
        /// </summary>
        private void ForceValidateProjectDirectory()
        {
            
        }
    }
}
