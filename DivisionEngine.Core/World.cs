using DivisionEngine.Components;
using DivisionEngine.Serialization;
using System.Reflection;

namespace DivisionEngine
{
    /// <summary>
    /// Stores all entities, components, and systems. This is the main ECS API.
    /// </summary>
    public class World
    {
        /// <summary>
        /// The name of this world.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// All entities in the world.
        /// </summary>
        public HashSet<uint> entities;

        /// <summary>
        /// Next ID used to register a new entity.
        /// </summary>
        public uint NextEntityId { get; set; }

        /// <summary>
        /// All componenents in the world organized by component type => entity => component data.
        /// </summary>
        public Dictionary<Type, Dictionary<uint, IComponent>> components;

        /// <summary>
        /// All systems in the world.
        /// </summary>
        public List<SystemBase> systems;

        private readonly List<SystemBase> awakeSystems, updateSystems, fixedUpdateSystems, unloadSystems, renderSystems;

        /// <summary>
        /// Create a new world.
        /// </summary>
        public World(string name)
        {
            Name = name;
            entities = [];
            components = [];
            systems = [];

            awakeSystems = [];
            updateSystems = [];
            fixedUpdateSystems = [];
            unloadSystems = [];
            renderSystems = [];
            NextEntityId = 0;

            RegisterAllSystems();
        }

        #region entities

        /// <summary>
        /// Check if an entity exists in the world.
        /// </summary>
        /// <param name="id">Entity id to check</param>
        /// <returns>Whether the entity exists in the world</returns>
        public bool EntityExists(uint id) => entities.Contains(id);

        /// <summary>
        /// Creates a new entity in the world.
        /// </summary>
        /// <returns>The new entity id created</returns>
        public uint CreateEntity()
        {
            uint id = NextEntityId;
            entities.Add(id);
            NextEntityId++;
            return id;
        }

        /// <summary>
        /// Creates a new entity in the world with a transform component.
        /// </summary>
        /// <returns>The new entity id created</returns>
        public uint CreateTransformEntity()
        {
            uint id = CreateEntity();
            AddComponent(id, new Transform());
            return id;
        }

        /// <summary>
        /// Creates a new entity in the world.
        /// </summary>
        /// <param name="name">The name of the entity</param>
        /// <returns>The new entity id created</returns>
        public uint CreateEntity(string name)
        {
            uint id = CreateEntity();
            AddComponent(id, new Name(name));
            return id;
        }

        /// <summary>
        /// Creates a new entity in the world with a transform component.
        /// </summary>
        /// <param name="name">The name of the entity</param>
        /// <returns>The new entity id created</returns>
        public uint CreateTransformEntity(string name)
        {
            uint id = CreateEntity(name);
            AddComponent(id, new Name(name));
            AddComponent(id, new Transform());
            return id;
        }

        /// <summary>
        /// Destroy an entity in the world.
        /// </summary>
        /// <param name="entityId">Entity to destroy</param>
        /// <returns>Whether entity of <paramref name="entityId"/> was destroyed.</returns>
        public bool DestroyEntity(uint entityId)
        {
            if (entities.Remove(entityId))
            {
                foreach (Type t in components.Keys)
                {
                    components[t].Remove(entityId);    
                }
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Attempts to get the entity's name from name component.
        /// </summary>
        /// <param name="entityId">Entity to find name for</param>
        /// <returns>Entity name if exists, otherwise empty string</returns>
        public string TryGetEntityName(uint entityId)
        {
            if (HasComponent<Name>(entityId))
            {
                Name nameComp = GetComponent<Name>(entityId)!;
                return nameComp.name!;
            }
            return string.Empty;
        }

        #endregion
        #region systems

        /// <summary>
        /// Searches all assemblies in the Application Domain to find all classes that inherit from SystemBase and registers them automatically.
        /// </summary>
        /// <exception cref="NotImplementedException">Throws an exception if a system is not implemented correctly</exception>
        public void RegisterAllSystems()
        {
            systems.Clear();
            awakeSystems.Clear();
            updateSystems.Clear();
            fixedUpdateSystems.Clear();
            unloadSystems.Clear();
            renderSystems.Clear();

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in a.GetTypes())
                {
                    if (typeof(SystemBase).IsAssignableFrom(t) && !t.IsAbstract)
                    {
                        if (Activator.CreateInstance(t) is SystemBase sys)
                        {
                            RegisterSystem(sys);
                        }
                        else throw new NotImplementedException($"System of type {t} is not implemented correctly!");
                    }
                }
            }
        }

        /// <summary>
        /// Registers a system and adds it to the correct callback loops.
        /// </summary>
        /// <param name="system">System to register</param>
        private void RegisterSystem(SystemBase system)
        {
            systems.Add(system);

            Type sysBaseType = typeof(SystemBase), sysType = system.GetType();
            MethodInfo? awakeInfo = sysType.GetMethod("Awake"),
                updateInfo = sysType.GetMethod("Update"),
                fixedUpdateInfo = sysType.GetMethod("FixedUpdate"),
                unloadInfo = sysType.GetMethod("Unload"),
                renderInfo = sysType.GetMethod("Render");

            if (awakeInfo != null && awakeInfo.DeclaringType != sysBaseType)
                awakeSystems.Add(system);
            if (updateInfo != null && updateInfo.DeclaringType != sysBaseType)
                updateSystems.Add(system);
            if (fixedUpdateInfo != null && fixedUpdateInfo.DeclaringType != sysBaseType)
                fixedUpdateSystems.Add(system);
            if (unloadInfo != null && unloadInfo.DeclaringType != sysBaseType)
                unloadSystems.Add(system);
            if (renderInfo != null && renderInfo.DeclaringType != sysBaseType)
                renderSystems.Add(system);
        }

        /// <summary>
        /// Calls all systems that implement "Awake".
        /// </summary>
        public void CallAwake()
        {
            for (int i = 0; i < awakeSystems.Count; i++)
                awakeSystems[i].Awake();
        }

        /// <summary>
        /// Calls all systems that implement "Update".
        /// </summary>
        public void CallUpdate()
        {
            for (int i = 0; i < updateSystems.Count; i++)
                updateSystems[i].Update();
        }

        /// <summary>
        /// Calls all systems that implement "FixedUpdate".
        /// </summary>
        public void CallFixedUpdate()
        {
            for (int i = 0; i < fixedUpdateSystems.Count; i++)
                fixedUpdateSystems[i].FixedUpdate();
        }

        /// <summary>
        /// Calls all systems that implement "FixedUpdate".
        /// </summary>
        public void CallUnload()
        {
            for (int i = 0; i < unloadSystems.Count; i++)
                unloadSystems[i].Unload();
        }

        /// <summary>
        /// Calls all systems that implement "Render".
        /// </summary>
        public void CallRender()
        {
            for (int i = 0; i < renderSystems.Count; i++)
                renderSystems[i].Render();
        }

        #endregion
        #region components

        /// <summary>
        /// Adds a component onto an entity in the world.
        /// </summary>
        /// <typeparam name="T">Component type to add</typeparam>
        /// <param name="entityId">Entity to add component to</param>
        /// <param name="component">Component data to add</param>
        /// <returns>True if the component was added</returns>
        public bool AddComponent<T>(uint entityId, T component) where T : IComponent
        {
            if (!entities.Contains(entityId))
                return false;

            Type type = typeof(T);
            if (components.ContainsKey(type))
            {
                if (!components[type].ContainsKey(entityId))
                {
                    components[type].Add(entityId, component);
                    return true;
                }
                return false;
            }

            Dictionary<uint, IComponent> temp = new Dictionary<uint, IComponent>
            {
                { entityId, component }
            };
            components.Add(type, temp);
            return true;
        }

        /// <summary>
        /// Removes a component from an entity in the world.
        /// </summary>
        /// <typeparam name="T">Component to remove</typeparam>
        /// <param name="entityId">Entity to remove component from</param>
        /// <returns>True if the component was removed</returns>
        public bool RemoveComponent<T>(uint entityId) where T : IComponent
        {
            Type type = typeof(T);
            if (entities.Contains(entityId) && components.TryGetValue(type, out var value))
            {
                bool removed = value.Remove(entityId);
                if (value.Count < 1)
                    components.Remove(type);
                return removed;
            }
            return false;
        }

        /// <summary>
        /// Gets a component on an entity.
        /// </summary>
        /// <typeparam name="T">Type of component to get</typeparam>
        /// <param name="entityId">The entity</param>
        /// <returns>The component on the entity</returns>
        public T? GetComponent<T>(uint entityId) where T : IComponent
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out var value) && value.TryGetValue(entityId, out var component))
                return (T)component;
            return default;
        }

        /// <summary>
        /// Gets all components on a specific entity.
        /// </summary>
        /// <param name="entityId">Entity ID to check</param>
        /// <returns>List of all components on entity of <paramref name="entityId"/></returns>
        public List<IComponent> GetAllComponents(uint entityId)
        {
            List<IComponent> entityComponents = [];
            foreach (var componentType in components.Values)
            {
                if (componentType.TryGetValue(entityId, out IComponent? component))
                    entityComponents.Add(component);
            }
            return entityComponents;
        }

        /// <summary>
        /// Checks if an entity has a component of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of component to check for</typeparam>
        /// <param name="entityId">Entity ID to check on</param>
        /// <returns>Whether the entity of <paramref name="entityId"/> has a component type <typeparamref name="T"/></returns>
        public bool HasComponent<T>(uint entityId) where T : IComponent
        {
            Type type = typeof(T);
            return components.ContainsKey(type) && components[type].ContainsKey(entityId);
        }

        /// <summary>
        /// Adds a component to the world from ComponentData serialized data.
        /// </summary>
        /// <param name="entityId">Entity to add component to</param>
        /// <param name="componentData">ComponentData object to parse and add</param>
        public void AddComponentFromData(uint entityId, ComponentData componentData)
        {
            try
            {
                // Find component type
                Type? componentType = Type.GetType($"{componentData.TypeName}, {componentData.AssemblyName}") ?? AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .FirstOrDefault(t => t.Name == componentData.TypeName &&
                                             typeof(IComponent).IsAssignableFrom(t));
                if (componentType == null || !typeof(IComponent).IsAssignableFrom(componentType))
                {
                    Debug.Error($"Component type not found: {componentData.TypeName}");
                    return;
                }

                // Create component instance
                if (Activator.CreateInstance(componentType) is not IComponent component)
                {
                    Debug.Error($"Failed to create component: {componentData.TypeName}");
                    return;
                }

                // Set properties from serialized data
                SetComponentProperties(component, componentData.Properties);

                // Add to world, call generic AddComponent
                var addMethod = GetType().GetMethod("AddComponent")!
                    .MakeGenericMethod(componentType);
                addMethod.Invoke(this, [entityId, component]);
            }
            catch (Exception ex)
            {
                Debug.Error($"Failed to load component {componentData.TypeName}: {ex.Message}");
            }
        }

        private static void SetComponentProperties(IComponent component, Dictionary<string, string> properties)
        {
            Type componentType = component.GetType();
            foreach (var kvp in properties)
            {
                FieldInfo? field = componentType.GetField(kvp.Key);
                if (field == null)
                {
                    Debug.Warning($"Field {kvp.Key} not found in {componentType.Name}");
                    continue;
                }

                try
                {
                    object? value = ParsePropertyValue(kvp.Value, field.FieldType);
                    if (value != null)
                        field.SetValue(component, value);
                }
                catch (Exception ex)
                {
                    Debug.Error($"Failed to set field {kvp.Key}: {ex.Message}");
                }
            }
        }

        private static object? ParsePropertyValue(string value, Type targetType)
        {
            if (value == "null") return null;

            // Handle custom types
            if (targetType == typeof(float3))
            {
                // Parse format: "(1,2,3)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 3)
                {
                    return new float3(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2])
                    );
                }
            }
            else if (targetType == typeof(float4))
            {
                // Parse format: "(1,2,3,4)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 4)
                {
                    return new float4(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                }
            }
            else if (targetType == typeof(float4x4))
            {
                // Parse format: "(1,0,0,0,0,1,0,0,0,0,1,0,0,0,-7,1)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 16)
                {
                    return new float4x4(
                        float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]),
                        float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]),
                        float.Parse(parts[8]), float.Parse(parts[9]), float.Parse(parts[10]), float.Parse(parts[11]),
                        float.Parse(parts[12]), float.Parse(parts[13]), float.Parse(parts[14]), float.Parse(parts[15])
                    );
                }
            }
            else if (targetType == typeof(string))
            {
                // Remove quotes if still present
                if (value.StartsWith('"') && value.EndsWith('"'))
                    return value.Trim('"');
                return value;
            }
            else if (targetType == typeof(bool))
                return bool.Parse(value);
            else if (targetType == typeof(float))
                return float.Parse(value);
            else if (targetType == typeof(int))
                return int.Parse(value);
            else if (targetType.IsEnum)
                return Enum.Parse(targetType, value);

            Debug.Warning($"Unhandled property parse type: {targetType.Name}");
            return null;
        }

        #endregion
        #region queries

        /// <summary>
        /// Queries the world to find entities with a component.
        /// </summary>
        /// <typeparam name="T">Type of components to find</typeparam>
        /// <returns>All entities with component type <typeparamref name="T"/></returns>
        public IEnumerable<uint> Query<T>() where T : IComponent
        {
            Type type = typeof(T);
            if (components.Count > 0 && components.TryGetValue(type, out Dictionary<uint, IComponent>? value))
                return value.Keys;
            return [];
        }

        /// <summary>
        /// Queries the world to find entities with certain component types.
        /// </summary>
        /// <param name="componentTypes">Component types to discover</param>
        /// <returns>List of entities with component types</returns>
        public IEnumerable<uint> Query(params Type[] componentTypes)
        {
            if (componentTypes.Length == 0) return [];

            IEnumerable<uint> queryResult = GetComponentStore(componentTypes[0]).Keys;
            for (int i = 1; i < componentTypes.Length; i++)
                queryResult = queryResult.Intersect(GetComponentStore(componentTypes[i]).Keys);
            return queryResult;
        }

        /// <summary>
        /// Queries the world to find entities with component types and returns the component data.
        /// </summary>
        /// <typeparam name="T">Type of component to search for</typeparam>
        /// <returns>Tuple of (entityId, T component)</returns>
        public IEnumerable<(uint, T)> QueryData<T>() where T : IComponent
        {
            foreach (uint entityId in Query<T>())
            {
                T queryResultComponent = (T)components[typeof(T)][entityId];
                yield return (entityId, queryResultComponent);
            }
        }

        public IEnumerable<(uint, T1, T2)> QueryData<T1, T2>()
            where T1 : IComponent where T2 : IComponent
        {
            foreach (uint entityId in Query(typeof(T1), typeof(T2)))
            {
                T1 queryResultComponent1 = (T1)components[typeof(T1)][entityId];
                T2 queryResultComponent2 = (T2)components[typeof(T2)][entityId];
                yield return (entityId, queryResultComponent1, queryResultComponent2);
            }
        }

        public IEnumerable<(uint, T1, T2, T3)> QueryData<T1, T2, T3>()
            where T1 : IComponent where T2 : IComponent where T3 : IComponent
        {
            foreach (uint entityId in Query(typeof(T1), typeof(T2), typeof(T3)))
            {
                T1 queryResultComponent1 = (T1)components[typeof(T1)][entityId];
                T2 queryResultComponent2 = (T2)components[typeof(T2)][entityId];
                T3 queryResultComponent3 = (T3)components[typeof(T3)][entityId];
                yield return (entityId, queryResultComponent1, queryResultComponent2, queryResultComponent3);
            }
        }

        public IEnumerable<(uint, IComponent[])> QueryData(params Type[] componentTypes)
        {
            foreach (uint entityId in Query(componentTypes))
            {
                IComponent[] queryResultComponents = new IComponent[componentTypes.Length];
                for (int i = 0; i < componentTypes.Length; i++)
                    queryResultComponents[i] = components[componentTypes[i]][entityId];

                yield return (entityId, queryResultComponents);
            }
        }

        private Dictionary<uint, IComponent> GetComponentStore(Type t) => components.GetValueOrDefault(t, []);

        #endregion
    }
}
