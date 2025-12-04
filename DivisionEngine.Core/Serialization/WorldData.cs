using System.Text.Json.Serialization;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Class used for serializing the world data to a project file.
    /// </summary>
    public class WorldData
    {
        public string Name { get; }
        public uint NextEntityId { get; }
        public List<EntityData> Entities { get; }

        /// <summary>
        /// Shortcut for "new WorldData(WorldManager.CurrentWorld!)".
        /// </summary>
        [JsonIgnore] public static WorldData Current => new WorldData(WorldManager.CurrentWorld!);

        /// <summary>
        /// Builds serializable world data object automatically from ECS world.
        /// </summary>
        /// <param name="world">World object to serialize</param>
        public WorldData(World world)
        {
            Name = world.Name;
            NextEntityId = world.NextEntityId;
            Entities = [];
            foreach (uint entity in world.entities)
                Entities.Add(new EntityData(entity, world));
        }
    }
}
