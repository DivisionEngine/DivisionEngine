using ComputeSharp.Generated;
using System.Text.Json.Serialization;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Class used for serializing the world data to a project file.
    /// </summary>
    public class WorldData
    {
        [JsonInclude] public string Name { get; }
        [JsonInclude] public uint NextEntityId { get; }
        [JsonInclude] public List<EntityData> Entities { get; }
        [JsonInclude] public DateTime Created { get; }
        [JsonInclude] public DateTime LastSaved { get; }

        [JsonIgnore] private object? serializedData;

        /// <summary>
        /// Builds serializable world data object automatically from ECS world.
        /// </summary>
        /// <param name="name">Name of the world</param>
        /// <param name="world">World object to serialize</param>
        public WorldData(string name, World world)
        {
            Name = name;
            NextEntityId = world.NextEntityId;
            Created = DateTime.Now;
            LastSaved = DateTime.Now;
            Entities = [];
            foreach (uint entity in world.entities)
                Entities.Add(new EntityData(entity, world));
        }

        public object Serialize()
        {
            serializedData = this; // Just return this object for JSON serialization
            return this;
        }

        public void Deserialize()
        {
            // This method would be called after JSON deserialization
            // You could add validation or post-processing here
            if (NextEntityId == 0 && Entities.Count > 0)
            {
                // Auto-calculate next entity ID if not set
                // NextEntityId = (uint)Entities.Max(e => e.Id) + 1;
            }
        }
    }
}
