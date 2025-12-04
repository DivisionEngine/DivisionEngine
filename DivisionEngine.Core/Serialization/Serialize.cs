using System.Reflection;
using System.Text.Json;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Handles serialization for Division Engine, fills in the gaps missing from System.Text.Json.
    /// </summary>
    public static class Serialize
    {
        /// <summary>
        /// Default serialization settings.
        /// </summary>
        private static JsonSerializerOptions DefaultSerializationSettings => new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        /// <summary>
        /// Gets the default json serialization of an object.
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Serialized json representation of object</returns>
        public static string Default(object? obj) => JsonSerializer.Serialize(obj, DefaultSerializationSettings);

        /// <summary>
        /// Serializes a component (ensures correct formatting of fields).
        /// </summary>
        /// <param name="component">Component to serialize</param>
        /// <returns>Serialized dictionary where keys are field names and values are serialized field values</returns>
        public static Dictionary<string, string> Component(IComponent component)
        {
            FieldInfo[] fields = component.GetType().GetFields();
            Dictionary<string, string> serialized = [];

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                object? fieldVal = field.GetValue(component);
                if (fieldVal == null)
                    serialized.Add(field.Name, "null");
                else
                {
                    // Adaptive serialization for some special types
                    string serializedField = fieldVal.ToString()!;

                    if (fieldType == typeof(float2))
                    {
                        float2 vec = (float2)fieldVal;
                        serializedField = $"({vec.X},{vec.Y})";
                    }
                    else if (fieldType == typeof(float3))
                    {
                        float3 vec = (float3)fieldVal;
                        serializedField = $"({vec.X},{vec.Y},{vec.Z})";
                    }
                    else if (fieldType == typeof(float4))
                    {
                        float4 vec = (float4)fieldVal;
                        serializedField = $"({vec.X},{vec.Y},{vec.Z},{vec.W})";
                    }
                    else if (fieldType == typeof(float4x4))
                    {
                        float4x4 matrix = (float4x4)fieldVal;
                        serializedField = Serialize4x4Matrix(matrix);
                    }

                    Debug.Warning(field.Name + " : " + serializedField);
                    serialized.Add(field.Name, serializedField);
                }
            }
            return serialized;
        }

        private static string Serialize4x4Matrix(float4x4 matrix) => 
            $"({matrix.M11},{matrix.M12},{matrix.M13},{matrix.M14}," +
            $"{matrix.M21},{matrix.M22},{matrix.M23},{matrix.M24}," +
            $"{matrix.M31},{matrix.M32},{matrix.M33},{matrix.M34}," +
            $"{matrix.M41},{matrix.M42},{matrix.M43},{matrix.M44})";
    }
}
