using DivisionEngine.Serialization;

namespace DivisionEngine.Projects
{
    /// <summary>
    /// Handles project state management in Division Engine.
    /// </summary>
    public class ProjectManager
    {
        /// <summary>
        /// Current project name.
        /// </summary>
        public static string? CurrentProjectName { get; private set; } = null;

        /// <summary>
        /// Current project directory path.
        /// </summary>
        public static string? CurrentProjectPath { get; private set; } = null;

        /// <summary>
        /// If a project is currently loaded.
        /// </summary>
        public static bool IsCurrentLoaded =>
            !string.IsNullOrWhiteSpace(CurrentProjectPath) && !string.IsNullOrWhiteSpace(CurrentProjectName);

        /// <summary>
        /// Searches project directory to find the project 
        /// </summary>
        /// <param name="projDir"></param>
        /// <returns></returns>
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

        public static bool LoadProject(string projDir)
        {
            CurrentProjectPath = projDir;
            CurrentProjectName = GetProjectName(projDir);

            if (IsCurrentLoaded)
            {
                // Force project validation
                bool validationStep = ForceValidateProjectDirectory(CurrentProjectName!, projDir);
                if (!validationStep)
                {
                    Debug.Error($"Project Failed Validation! | Path: {projDir}");
                    return false;
                }

                DivisionProject? tempProjectData = null;
                // Add deseralization and loading here
                foreach (string projPath in Directory.EnumerateFiles(projDir, "*.divproj", SearchOption.TopDirectoryOnly))
                {
                    string projJson = File.ReadAllText(projPath);
                    if (!string.IsNullOrEmpty(projJson))
                        tempProjectData = Deserialize.Default<DivisionProject>(projJson);
                    break; // Break after first project file
                }

                if (tempProjectData != null)
                {
                    Debug.Info("Project Manager: Loaded project settings.");
                    LoadProjectData(tempProjectData);
                }

                WorldData? tempWorldData = null;
                // Add deseralization and loading here
                foreach (string worldPath in Directory.EnumerateFiles(projDir, "*.wld", SearchOption.TopDirectoryOnly))
                {
                    string worldJson = File.ReadAllText(worldPath);
                    if (!string.IsNullOrEmpty(worldJson))
                        tempWorldData = Deserialize.Default<WorldData>(worldJson);
                    break; // Break after first world found for now
                }

                if (tempWorldData != null)
                {
                    Debug.Info("Project Manager: World data deserialized.");
                    LoadWorldDataIntoCurrent(tempWorldData);
                }

                return true;
            }
            return false;
        }

        private static void LoadProjectData(DivisionProject projectData)
        {
            // Project settings can be loaded here eventually.
        }

        private static void LoadWorldDataIntoCurrent(WorldData worldData)
        {
            World newWorld = new World(worldData.Name);
            newWorld.entities = new HashSet<uint>();
            for (int i = 0; i < worldData.Entities.Count; i++)
            {
                newWorld.entities.Add(worldData.Entities[i].Id);
                Debug.Warning($"Added entity: {worldData.Entities[i].Id}");
            }

            // Make current world
            WorldManager.SetWorld(newWorld);
            WorldManager.SwitchWorld(newWorld.Name);
        }

        public static bool SaveNewProject(string projName, string projDir)
        {
            if (!string.IsNullOrWhiteSpace(projDir) && !string.IsNullOrEmpty(projName))
            {
                CurrentProjectName = projName;
                CurrentProjectPath = projDir;
                return SaveProject(projName, projDir);
            }
            return false;
        }

        public static bool SaveCurrentProject()
        {
            if (IsCurrentLoaded)
                return SaveProject(CurrentProjectName!, CurrentProjectPath!);
            return false;
        }

        private static bool SaveProject(string projName, string projDir)
        {
            if (WorldManager.CurrentWorld != null)
            {
                // Force project validation
                bool validationStep = ForceValidateProjectDirectory(projName, projDir);
                if (!validationStep)
                {
                    Debug.Error($"Project Failed Validation! | Path: {projDir}");
                    return false;
                }

                // Serialize world
                WorldData worldData = WorldData.Current;
                string serializedWorld = Serialize.Default(worldData);

                // Create project data
                DivisionProject projectData = new DivisionProject(projName);
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
