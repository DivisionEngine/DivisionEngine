using System.Reflection;

namespace DivisionEngine
{
    public class World
    {
        public HashSet<uint> entities;
        public Dictionary<Type, Dictionary<uint, IComponent>> components;
        public List<SystemBase> systems;

        private List<SystemBase> awakeSystems, updateSystems, fixedUpdateSystems, renderSystems;
        private uint nextEntityId;

        public World()
        {
            entities = [];
            components = [];
            systems = [];

            awakeSystems = [];
            updateSystems = [];
            fixedUpdateSystems = [];
            renderSystems = [];
            nextEntityId = 0;
        }

        #region entities

        public uint CreateEntity()
        {
            uint id = nextEntityId;
            entities.Add(id);
            nextEntityId++;
            return id;
        }

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

        #endregion
        #region systems

        public void RegisterAllSystems()
        {
            systems.Clear();
            awakeSystems.Clear();
            updateSystems.Clear();
            fixedUpdateSystems.Clear();
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

        private void RegisterSystem(SystemBase system)
        {
            systems.Add(system);

            Type sysBaseType = typeof(SystemBase), sysType = system.GetType();
            MethodInfo? awakeInfo = sysType.GetMethod("Awake"),
                updateInfo = sysType.GetMethod("Update"),
                fixedUpdateInfo = sysType.GetMethod("FixedUpdate"),
                renderInfo = sysType.GetMethod("Render");

            if (awakeInfo != null && awakeInfo.DeclaringType != sysBaseType)
                awakeSystems.Add(system);
            if (updateInfo != null && updateInfo.DeclaringType != sysBaseType)
                updateSystems.Add(system);
            if (fixedUpdateInfo != null && fixedUpdateInfo.DeclaringType != sysBaseType)
                fixedUpdateSystems.Add(system);
            if (renderInfo != null && renderInfo.DeclaringType != sysBaseType)
                renderSystems.Add(system);
        }

        public void CallAwake()
        {
            for (int i = 0; i < awakeSystems.Count; i++)
                awakeSystems[i].Awake();
        }

        public void CallUpdate()
        {
            for (int i = 0; i < updateSystems.Count; i++)
                updateSystems[i].Update();
        }

        public void CallFixedUpdate()
        {
            for (int i = 0; i < fixedUpdateSystems.Count; i++)
                fixedUpdateSystems[i].FixedUpdate();
        }

        public void CallRender()
        {
            for (int i = 0; i < renderSystems.Count; i++)
                renderSystems[i].Render();
        }

        #endregion
        #region components

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

        public T GetComponent<T>(uint entityId) where T : IComponent
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out var value) && value.TryGetValue(entityId, out var component))
                return (T)component;
            throw new InvalidOperationException($"Entity {entityId} does not have {type.Name} component.");
        }

        public bool HasComponent<T>(uint entityId) where T : IComponent
        {
            Type type = typeof(T);
            return components.ContainsKey(type) && components[type].ContainsKey(entityId);
        }

        #endregion
        #region queries

        public IEnumerable<uint> Query<T>() where T : IComponent
        {
            Type type = typeof(T);
            if (components.Count > 0 && components.ContainsKey(type))
                return components[type].Keys;
            return [];
        }

        #endregion
    }
}
