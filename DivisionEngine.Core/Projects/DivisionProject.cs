using System.Text.Json.Serialization;

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

        [JsonConstructor]
        public DivisionProject()
        {
            Name = string.Empty;
            Version = string.Empty;
        }

        public DivisionProject(string name = "New Project")
        {
            LastSaved = DateTime.Now;
            Name = name;
            Version = "1.0.0";
        }
    }
}
