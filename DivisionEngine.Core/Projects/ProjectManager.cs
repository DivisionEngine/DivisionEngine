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
        /// Searches project directory to find the project name.
        /// </summary>
        /// <param name="projDir">Project directory to search</param>
        /// <returns>Project file name</returns>
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

        /// <summary>
        /// Gets the project file path from the project directory.
        /// </summary>
        /// <param name="projDir">Project top level directory</param>
        /// <returns>The path of the project file</returns>
        public static string GetProjectPath(string projDir) => $"{projDir}\\{GetProjectName(projDir)!}.divproj";

        /// <summary>
        /// Gets the project file path from the project directory and project name.
        /// </summary>
        /// <param name="projDir">Project top level directory</param>
        /// <param name="projName">Project name</param>
        /// <returns>The path of the project file</returns>
        public static string GetProjectPath(string projDir, string projName) => $"{projDir}\\{projName}.divproj";

        /// <summary>
        /// Gets the path to the world data file.
        /// </summary>
        /// <param name="projDir">Project to look in</param>
        /// <param name="world">WorldData to find path for</param>
        /// <returns>Formatted world data file path</returns>
        public static string GetWorldPath(string projDir, WorldData world) => $"{projDir}\\{world.Name}.wld";

        /// <summary>
        /// Loads a project via its top level directory.
        /// </summary>
        /// <param name="projDir">Project directory to load</param>
        /// <returns>If the project was successfully loaded</returns>
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
                
                // Load project file
                DivisionProject? tempProjectData = null;
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

                // Load world data file
                WorldData? tempWorldData = null;
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

        /// <summary>
        /// Loads a DivsionProject object into the current project.
        /// </summary>
        /// <param name="projectData">Project settings to parse and load</param>
        private static void LoadProjectData(DivisionProject projectData)
        {
            // Project settings can be loaded here eventually.
            
        }

        /// <summary>
        /// Loads a WorldData object into the current world.
        /// </summary>
        /// <param name="worldData">WorldData to parse and load</param>
        private static void LoadWorldDataIntoCurrent(WorldData worldData)
        {
            World newWorld = new World(worldData.Name)
            {
                entities = [],
                NextEntityId = worldData.NextEntityId
            };

            // Create entities and components
            foreach (EntityData entityData in worldData.Entities)
            {
                newWorld.entities.Add(entityData.Id);
                foreach (ComponentData componentData in entityData.Components)
                    newWorld.AddComponentFromData(entityData.Id, componentData);
            }

            // Register systems
            newWorld.RegisterAllSystems();

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
