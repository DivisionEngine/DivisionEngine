namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Stores entity data for serializing project entities.
    /// </summary>
    public class EntityData
    {
        public uint Id { get; }
        public List<ComponentData> Components { get; }

        public EntityData(uint entity, World world)
        {
            Id = entity;
            Components = [];
            List<IComponent> comps = world.GetAllComponents(entity);
            for (int i = 0; i < comps.Count; i++)
                Components.Add(new ComponentData(comps[i]));
        }
    }
}
