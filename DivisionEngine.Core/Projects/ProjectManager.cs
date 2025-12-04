using DivisionEngine.Serialization;

namespace DivisionEngine.Projects
{
    /// <summary>
    /// Handles project state management in Division Engine.
    /// </summary>
    public class ProjectManager
    {
        public static string? GetProjectName(string projDir)
        {
            DirectoryInfo projDirInfo = new DirectoryInfo(projDir);
            if (projDirInfo.Exists)
            {
                foreach (FileInfo file in projDirInfo.EnumerateFiles("*.divproj", SearchOption.TopDirectoryOnly))
                    return file.Name;
            }
            return null;
        }

        public static string GetProjectPath(string projDir) => $"{projDir}\\{GetProjectName(projDir)!}.divproj";
        public static string GetProjectPath(string projDir, string projName) => $"{projDir}\\{projName}.divproj";

        public static string GetWorldPath(string projDir, WorldData world) => $"{projDir}\\{world.Name}.wld";

        public static void LoadProject(string projDir)
        {
            // Implement project deseralization
        }

        public static bool SaveCurrentProject(string projName, string projDir)
        {
            if (!string.IsNullOrEmpty(projDir) && WorldManager.CurrentWorld != null)
            {
                // Force project validation
                bool validationStep = ForceValidateProjectDirectory(projName, projDir);
                if (!validationStep) Debug.Error($"Project Failed Validation! | Path: {projDir}");

                // Serialize world
                WorldData worldData = WorldData.Current;
                string serializedWorld = Serialize.Default(worldData);

                // Create project data
                DivisionProject projectData = new DivisionProject(projDir, projName);
                string serializedProjectData = Serialize.Default(projectData);

                // Write project file
                File.WriteAllText(GetProjectPath(projDir, projName), serializedProjectData);

                // Write single world file
                File.WriteAllText(GetWorldPath(projDir, worldData), serializedWorld);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Directory setup should be:
        /// Project Folder/
        /// - project.divproj
        /// - world.wld
        /// - Assets/
        ///     - example.png
        /// </summary>
        /// <param name="projName">The name of the project folder</param>
        /// <param name="projectDir">The directiory of the project</param>
        /// <returns>Whether the project directory formatting executed successfully</returns>
        private static bool ForceValidateProjectDirectory(string projName, string projectDir)
        {
            if (!string.IsNullOrEmpty(projName) && !string.IsNullOrEmpty(projectDir))
            {
                // Validate project directory
                DirectoryInfo projDirInfo = new DirectoryInfo(projectDir);
                if (!projDirInfo.Exists)
                {
                    projDirInfo.Create();
                    Debug.Info($"Created Project Directory: {projDirInfo.FullName}");
                }

                // Validate assets directory
                DirectoryInfo assetsDir = new DirectoryInfo($"{projectDir}\\Assets\\");
                if (!assetsDir.Exists) assetsDir.Create();

                return true;
            }
            return false;
        }
    }
}
