using System.Text.Json.Serialization;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Represents serializable data for a component in Division Engine.
    /// </summary>
    public class ComponentData
    {
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        [JsonConstructor]
        public ComponentData()
        {
            TypeName = string.Empty;
            AssemblyName = string.Empty;
            Properties = [];
        }

        public ComponentData(IComponent component)
        {
            TypeName = component.GetType().Name;
            AssemblyName = component.GetType().Assembly.FullName!;
            Properties = Serialize.Component(component);
        }
    }
}
