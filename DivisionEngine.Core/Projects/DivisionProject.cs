namespace DivisionEngine.Projects
{
    /// <summary>
    /// Represents a project in the Division Engine, used for serializing project data.
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
    }
}
