//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
using DivisionEngine.Projects.Assets;
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
        /// Asset database currently in use.
        /// </summary>
        public static AssetDatabase? AssetDatabase { get; private set; }

        /// <summary>
        /// Asset manager currently in use.
        /// </summary>
        public static AssetManager? AssetManager { get; private set; }

        // Project Events
        public static event Action? ProjectLoaded;
        public static event Action? ProjectClosing;
        public static event Action? ProjectClosed;

        // Asset Events (forwarded from AssetDatabase for convenience)
        public static event Action<string>? AssetFolderChanged;
        public static event Action<AssetMetadata>? AssetAdded;
        public static event Action<AssetMetadata>? AssetRemoved;
        public static event Action<AssetMetadata>? AssetUpdated;

        /// <summary>
        /// Searches project directory to find the project file (ex. NewProject.divp).
        /// </summary>
        /// <param name="projDir">Project directory to search</param>
        /// <returns>Project file name (includes .divp extension)</returns>
        public static string? GetProjectFile(string projDir)
        {
            DirectoryInfo projDirInfo = new DirectoryInfo(projDir);
            if (projDirInfo.Exists)
            {
                foreach (FileInfo file in projDirInfo.EnumerateFiles("*.divp", SearchOption.TopDirectoryOnly))
                    return file.Name;
            }
            return null;
        }

        /// <summary>
        /// Gets the project file path from the project directory.
        /// </summary>
        /// <param name="projDir">Project top level directory</param>
        /// <returns>The path of the project file</returns>
        public static string GetProjectPath(string projDir) => $"{projDir}\\{GetProjectFile(projDir)!}";

        /// <summary>
        /// Gets the project file path from the project directory and project name.
        /// </summary>
        /// <param name="projDir">Project top level directory</param>
        /// <param name="projName">Project name</param>
        /// <returns>The path of the project file</returns>
        public static string GetProjectPath(string projDir, string projName) => $"{projDir}\\{projName}.divp";

        /// <summary>
        /// Gets the path to the world data file.
        /// </summary>
        /// <param name="projDir">Project to look in</param>
        /// <param name="world">WorldData to find path for</param>
        /// <returns>Formatted world data file path</returns>
        public static string GetWorldPath(string projDir, WorldData world) => $"{projDir}\\{world.Name}.wld";

        /// <summary>
        /// Checks to see if the project directory is a valid Division Engine project.
        /// </summary>
        /// <param name="projDir">Project directory to check</param>
        /// <returns>If the project directory is a Division Engine project</returns>
        public static bool IsDivisionProject(string projDir) => File.Exists(GetProjectPath(projDir));

        /// <summary>
        /// Loads a project via its top level directory.
        /// </summary>
        /// <param name="projDir">Project directory to load</param>
        /// <returns>If the project was successfully loaded</returns>
        public static bool LoadProject(string projDir)
        {
            CurrentProjectPath = projDir;
            CurrentProjectName = GetProjectFile(projDir)?.Replace(".divp", "");

            if (IsCurrentLoaded)
            {
                // Force project validation
                bool validationStep = ForceValidateProjectDirectory(CurrentProjectName!, projDir);
                if (!validationStep)
                {
                    Debug.Error($"Project Failed Validation! | Path: {projDir}");
                    return false;
                }

                // Initialize Asset System
                InitializeAssetSystem(projDir);

                // Load project file
                DivisionProject? tempProjectData = null;
                foreach (string projPath in Directory.EnumerateFiles(projDir, "*.divp", SearchOption.TopDirectoryOnly))
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

                    // After world is loaded, resolve any asset references in components
                    ResolveAssetReferencesInWorld(WorldManager.CurrentWorld);
                }

                ProjectLoaded?.Invoke(); // Notify that a project was loaded
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
        /// Resolves asset references in world components (called after world load).
        /// </summary>
        private static void ResolveAssetReferencesInWorld(World? world)
        {
            if (world == null || AssetManager == null) return;

            // This would iterate through all components and resolve AssetReference fields
            // For now, just log
            Debug.Info("Project Manager: Asset references ready for loading");
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

        /// <summary>
        /// Saves a new project with a specified name and project directory.
        /// </summary>
        /// <param name="projName">New project name</param>
        /// <param name="projDir">New project directory</param>
        /// <returns>If new project creation was successful</returns>
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

        /// <summary>
        /// Saves the current project.
        /// </summary>
        /// <returns>If a project is loaded</returns>
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
                 
                WorldData worldData = WorldData.Current; // Serialize world
                string serializedWorld = Serialize.Default(worldData);
                DivisionProject projectData = new DivisionProject(projName); // Create project data
                string serializedProjectData = Serialize.Default(projectData);

                File.WriteAllText(GetProjectPath(projDir, projName), serializedProjectData); // Write project file
                File.WriteAllText(GetWorldPath(projDir, worldData), serializedWorld); // Write single world file

                // Asset database doesn't need saving - it's auto-saved via .divmeta files

                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates correct project directory setup.
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

        /// <summary>
        /// Sets up the asset database and asset manager.
        /// </summary>
        /// <param name="projDir">Project directory</param>
        private static void InitializeAssetSystem(string projDir)
        {
            string assetsPath = Path.Combine(projDir, "Assets");

            // Create database (this scans all folders)
            AssetDatabase = new AssetDatabase(assetsPath);

            // Forward events from database to project-level events
            AssetDatabase.FolderChanged += AssetFolderChanged;
            AssetDatabase.AssetAdded += AssetAdded;
            AssetDatabase.AssetRemoved += AssetRemoved;
            AssetDatabase.AssetUpdated += AssetUpdated;

            // Create manager
            AssetManager = new AssetManager(AssetDatabase);

            Debug.Info($"Asset System initialized. Found {AssetDatabase.GetAllAssets().Count()} assets.");
        }

        /// <summary>
        /// Called when projects are closed.
        /// </summary>
        public static void CloseProject()
        {
            ProjectClosing?.Invoke(); // Start closing notify

            // Unload all assets first
            AssetManager?.UnloadAll();

            // Dispose database (stops file watcher)
            AssetDatabase?.Dispose();

            // Clear references
            AssetDatabase = null;
            AssetManager = null;
            CurrentProjectName = null;
            CurrentProjectPath = null;

            // Clear forwarded events
            AssetFolderChanged = null;
            AssetAdded = null;
            AssetRemoved = null;
            AssetUpdated = null;

            ProjectClosed?.Invoke();
        }

        // ------------------------
        // Asset Management Helpers
        // ------------------------

        /// <summary>
        /// Gets an asset by GUID, loading it if necessary.
        /// </summary>
        public static async Task<T?> GetAssetAsync<T>(string guid) where T : Asset
        {
            if (AssetManager == null)
            {
                Debug.Error("Cannot get asset: No project loaded");
                return null;
            }

            return await AssetManager.LoadAssetAsync<T>(guid);
        }

        /// <summary>
        /// Gets an asset by GUID (synchronous - asset must already be loaded).
        /// </summary>
        public static T? GetAsset<T>(string guid) where T : Asset
        {
            if (AssetManager == null) return null;

            // Check if already loaded without loading
            Asset? asset = AssetManager.Get(guid);
            return asset as T;
        }

        /// <summary>
        /// Unloads an asset when no longer needed.
        /// </summary>
        public static void ReleaseAsset(string guid)
        {
            AssetManager?.UnloadAsset(guid);
        }

        /// <summary>
        /// Gets metadata for an asset.
        /// </summary>
        public static AssetMetadata? GetAssetMetadata(string guid)
        {
            return AssetDatabase?.GetAssetMetadataByID(guid);
        }

        /// <summary>
        /// Gets all assets of a specific type.
        /// </summary>
        public static IEnumerable<AssetMetadata> GetAssetsByType(AssetType type)
        {
            return AssetDatabase?.GetAssetsByType(type) ?? [];
        }

        /// <summary>
        /// Imports a file into the project assets.
        /// </summary>
        public static AssetMetadata? ImportAsset(string sourceFilePath, string destinationFolder = "")
        {
            return AssetDatabase?.ImportAsset(sourceFilePath, destinationFolder);
        }

        /// <summary>
        /// Deletes an asset from the project.
        /// </summary>
        public static bool DeleteAsset(string guid)
        {
            // Unload if loaded
            AssetManager?.UnloadAsset(guid);

            // Delete from database
            return AssetDatabase?.DeleteAsset(guid) ?? false;
        }

        /// <summary>
        /// Refreshes the asset database (rescans all folders).
        /// </summary>
        public static void RefreshAssetDatabase()
        {
            AssetDatabase?.ScanAllFolders();
        }
    }
}
