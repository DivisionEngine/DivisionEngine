using System.Text.Json.Serialization;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Represents serializable data for a component in Division Engine.
    /// </summary>
    public class ComponentData
    {
        [JsonInclude] public string TypeName { get; }
        [JsonInclude] public string AssemblyName { get; }
        [JsonInclude] public Dictionary<string, object> Properties { get; }

        [JsonIgnore] private object? serializedData;

        public ComponentData(IComponent component)
        {
            TypeName = component.GetType().Name;
            AssemblyName = component.GetType().Assembly.FullName!;
            Properties = [];
        }

        public object Serialize()
        {
            if (string.IsNullOrEmpty(TypeName)) // Ensure TypeName and AssemblyName are set
            {
                throw new InvalidOperationException("ComponentData must have a TypeName");
            }

            serializedData = this;
            return this;
        }

        public void Deserialize()
        {

        }
    }
}
