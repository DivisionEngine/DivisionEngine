using DivisionEngine.Components;

namespace DivisionEngine
{
    /// <summary>
    /// Manages all worlds loaded and currently active.
    /// </summary>
    public static class WorldManager
    {
        /// <summary>
        /// Current active world.
        /// </summary>
        public static World? CurrentWorld { get; private set; }
        private static readonly Dictionary<string, World> worlds = [];

        /// <summary>
        /// Creates a new default world.
        /// </summary>
        /// <param name="makeCurrent">Makes the newly created default world the current world</param>
        /// <returns>The new default world</returns>
        public static World CreateDefaultWorld(bool makeCurrent)
        {
            World newDefaultWorld = new World();
            newDefaultWorld.RegisterAllSystems();

            uint cameraEntity = newDefaultWorld.CreateEntity();
            newDefaultWorld.AddComponent(cameraEntity, new Transform
            {
                position = new float3(0, 2, 5)
            });
            newDefaultWorld.AddComponent(cameraEntity, new Camera());
            newDefaultWorld.AddComponent(cameraEntity, new Player());

            uint sphereEntity = newDefaultWorld.CreateEntity();
            newDefaultWorld.AddComponent(sphereEntity, new Transform());
            newDefaultWorld.AddComponent(sphereEntity, new SDFSphere());

            SetWorld("default", newDefaultWorld);
            if (makeCurrent) CurrentWorld = newDefaultWorld;
            return newDefaultWorld;
        }

        /// <summary>
        /// Sets / adds a world based off a key.
        /// </summary>
        /// <param name="key">Key of the world to set / add</param>
        /// <param name="world">World to add / set</param>
        public static void SetWorld(string key, World world)
        {
            if (!worlds.TryAdd(key, world))
                worlds[key] = world;
        }

        /// <summary>
        /// Checks if a world exists in the world manager.
        /// </summary>
        /// <param name="key">Key to check for</param>
        /// <returns>If world linked to key exists</returns>
        public static bool HasWorld(string key) => worlds.ContainsKey(key);

        /// <summary>
        /// Gets a world based off a certain key.
        /// </summary>
        /// <param name="key">Key of the world to retrieve</param>
        /// <returns>The world referenced by key</returns>
        public static World? GetWorld(string key)
        {
            worlds.TryGetValue(key, out var world);
            return world;
        }

        /// <summary>
        /// Switches the current world to the one referenced by a certain key.
        /// </summary>
        /// <param name="key">Key of the world to make current</param>
        /// <returns>Whether or not the switch was successful</returns>
        public static bool SwitchWorld(string key)
        {
            if (worlds.TryGetValue(key, out var world))
            {
                CurrentWorld = world;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a world from the world manager.
        /// </summary>
        /// <param name="key">Key of the world to remove</param>
        /// <returns>Whether the world could be removed or not</returns>
        public static bool RemoveWorld(string key)
        {
            if (CurrentWorld == worlds[key])
                return false;
            return worlds.Remove(key);
        }
    }
}
