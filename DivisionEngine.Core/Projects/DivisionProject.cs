using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DivisionEngine.Editor.Projects
{
    public class DivisionProject
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string ProjectName { get; set; } = "New Project";
        public string ProjectVersion { get; set; } = "1.0.0";
        public DateTime LastSaved { get; set; } = DateTime.Now;

        [JsonIgnore]
        public string ProjectFilePath => Path.Combine(ProjectPath, $"{ProjectName}.division");

        [JsonIgnore]
        public string AssetsPath => Path.Combine(ProjectPath, "Assets");

        [JsonIgnore]
        public string SettingsPath => Path.Combine(ProjectPath, "ProjectSettings.json");


        public DivisionProject(string path, string name = "New Project")
        {
            ProjectPath = path;
            ProjectName = name;
        }

        private void Load()
        {
            // Implement loading project logic here
            if (!File.Exists(ProjectFilePath))
                throw new FileNotFoundException($"Project file not found: {ProjectFilePath}");

            try
            {
                string json = File.ReadAllText(ProjectFilePath);
                var project = JsonSerializer.Deserialize<DivisionProject>(json);

                if (project != null)
                {
                    ProjectName = project.ProjectName;
                    ProjectVersion = project.ProjectVersion;
                    LastSaved = project.LastSaved;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load project: {ex.Message}", ex);
            }
        }

        public void Save()
        {
            // Implement saving project logic here
        }
    }
}
