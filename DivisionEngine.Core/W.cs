namespace DivisionEngine
{
    /// <summary>
    /// Helper class for accessing the current world.
    /// </summary>
    public static class W
    {
        #region entities

        public static bool EntityExists(uint id) => WorldManager.CurrentWorld!.EntityExists(id);

        /// <summary>
        /// Creates a new entity in the current world.
        /// </summary>
        /// <returns>The new entity id created</returns>
        public static uint CreateEntity() => WorldManager.CurrentWorld!.CreateEntity();

        public static bool DestroyEntity(uint entityId) => WorldManager.CurrentWorld!.DestroyEntity(entityId);

        #endregion
        #region systems

        /// <summary>
        /// Searches all assemblies in the Application Domain to find all classes that inherit from SystemBase and registers them automatically.
        /// </summary>
        /// <exception cref="NotImplementedException">Throws an exception if a system is not implemented correctly</exception>
        public static void RegisterAllSystems() => WorldManager.CurrentWorld!.RegisterAllSystems();

        #endregion
        #region components

        public static bool AddComponent<T>(uint entityId, T component) where T : IComponent =>
            WorldManager.CurrentWorld!.AddComponent(entityId, component);

        public static bool RemoveComponent<T>(uint entityId) where T : IComponent =>
            WorldManager.CurrentWorld!.RemoveComponent<T>(entityId);

        public static T GetComponent<T>(uint entityId) where T : IComponent =>
            WorldManager.CurrentWorld!.GetComponent<T>(entityId);

        public static bool HasComponent<T>(uint entityId) where T : IComponent =>
            WorldManager.CurrentWorld!.HasComponent<T>(entityId);

        #endregion
        #region queries

        public static IEnumerable<uint> Query<T>() where T : IComponent => WorldManager.CurrentWorld!.Query<T>();

        public static IEnumerable<uint> Query(params Type[] componentTypes) => WorldManager.CurrentWorld!.Query(componentTypes);

        public static IEnumerable<(uint, T)> QueryData<T>() where T : IComponent => WorldManager.CurrentWorld!.QueryData<T>();

        public static IEnumerable<(uint, T1, T2)> QueryData<T1, T2>()
            where T1 : IComponent where T2 : IComponent =>
            WorldManager.CurrentWorld!.QueryData<T1, T2>();

        public static IEnumerable<(uint, T1, T2, T3)> QueryData<T1, T2, T3>()
            where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            WorldManager.CurrentWorld!.QueryData<T1, T2, T3>();

        public static IEnumerable<(uint, IComponent[])> QueryData(params Type[] componentTypes) =>
            WorldManager.CurrentWorld!.QueryData(componentTypes);

        #endregion
    }
}
