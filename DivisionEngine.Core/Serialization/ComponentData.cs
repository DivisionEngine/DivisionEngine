namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Represents serializable data for a component in Division Engine.
    /// </summary>
    public class ComponentData
    {
        public string TypeName { get; }
        public string AssemblyName { get; }
        public Dictionary<string, string> Properties { get; }

        public ComponentData(IComponent component)
        {
            TypeName = component.GetType().Name;
            AssemblyName = component.GetType().Assembly.FullName!;
            Properties = Serialize.Component(component);
        }
    }
}
