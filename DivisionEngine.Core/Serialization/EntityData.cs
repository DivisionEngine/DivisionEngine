using System.Text.Json.Serialization;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Stores entity data for serializing project entities.
    /// </summary>
    public class EntityData : ISerializable
    {
        [JsonInclude] public uint Id { get; }
        [JsonInclude] public List<ComponentData> Components { get; }

        [JsonIgnore] private object? serializedData;

        public EntityData(uint entity, World world)
        {
            Id = entity;
            Components = [];
            List<IComponent> comps = world.GetAllComponents(entity);
            for (int i = 0; i < comps.Count; i++)
                Components.Add(new ComponentData(comps[i]));
        }

        public object Serialize()
        {
            serializedData = this;
            return this;
        }

        public void Deserialize()
        {
            // Validation or post-processing after deserialization
            if (string.IsNullOrWhiteSpace(Name))
            {
                Name = $"Entity_{Id}";
            }
        }
    }
}
