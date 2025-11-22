namespace DivisionEngine
{
    public class World
    {
        public HashSet<uint> entities;
        public Dictionary<Type, Dictionary<uint, IComponent>> components;
        public List<ISystem> systems;

        private uint nextEntityId;

        public World()
        {
            entities = new HashSet<uint>();
            components = new Dictionary<Type, Dictionary<uint, IComponent>>();
            systems = new List<ISystem>();
            nextEntityId = 0;
        }

        public uint CreateEntity()
        {
            uint id = nextEntityId;
            entities.Add(id);
            nextEntityId++;
            return id;
        }

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
                return value.Remove(entityId);
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
    }
}
