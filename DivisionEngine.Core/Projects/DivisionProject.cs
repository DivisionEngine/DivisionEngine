using System.Text;
using System.Text.Json;

namespace DivisionEngine.Editor.Projects
{
    internal class DivisionProject
    {
        public string ProjectPath { get; private set; } = string.Empty;
        public string ProjectName { get; set; } = "New Project";
        public string ProjectVersion { get; set; } = "1.0.0";

        public DivisionProject(string path)
        {
            ProjectPath = path;
            Load();
        }

        private void Load()
        {
            // Implement loading project logic here
            
        }

        public void Save()
        {
            // Implement saving project logic here
        }
    }
}
