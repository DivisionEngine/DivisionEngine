namespace DivisionEngine
{
    /// <summary>
    /// Manages all ecs worlds loaded and currently active.
    /// </summary>
    public static class WorldManager
    {
        public static World? CurrentWorld { get; private set; }
        private static Dictionary<string, World> worlds = new Dictionary<string, World>();

        public static void SetWorld(string key, World world)
        {
            if (!worlds.TryAdd(key, world))
                worlds[key] = world;
        }

        public static bool HasWorld(string key) => worlds.ContainsKey(key);

        public static World? GetWorld(string key)
        {
            worlds.TryGetValue(key, out var world);
            return world;
        }

        public static bool SwitchWorld(string key)
        {
            if (worlds.TryGetValue(key, out var world))
            {
                CurrentWorld = world;
                return true;
            }
            return false;
        }

        public static bool RemoveWorld(string key)
        {
            if (CurrentWorld == worlds[key])
                return false;
            return worlds.Remove(key);
        }
    }
}
