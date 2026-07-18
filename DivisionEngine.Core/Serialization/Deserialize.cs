//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Projects.Assets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Handles deserialization for Division Engine objects.
    /// </summary>
    public static class Deserialize
    {
        /// <summary>
        /// Default serialization settings.
        /// </summary>
        private static JsonSerializerOptions DefaultDeserializationSettings => new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        /// <summary>
        /// Deserializes json text to an object using default settings.
        /// </summary>
        /// <typeparam name="T">Type of object to return</typeparam>
        /// <param name="text">Text to deserialize</param>
        /// <returns>Deserialized object of type <typeparamref name="T"/></returns>
        public static T? Default<T>(string text) => JsonSerializer.Deserialize<T>(text, DefaultDeserializationSettings);

        /// <summary>
        /// Deserializes component properties into an IComponent object.
        /// </summary>
        /// <param name="component">Deserialized component</param>
        /// <param name="properties">Properties to deserialize</param>
        public static void SetComponentProperties(IComponent component, Dictionary<string, string> properties)
        {
            Type componentType = component.GetType();
            foreach (var kvp in properties)
            {
                FieldInfo? field = componentType.GetField(kvp.Key);
                if (field == null)
                {
                    Debug.Warning($"Field {kvp.Key} not found in {componentType.Name}");
                    continue;
                }

                // Skip fields that shouldn't be deserialized
                if (!ShouldDeserializeField(field)) continue;

                try
                {
                    object? value = ParsePropertyValue(kvp.Value, field.FieldType);
                    if (value != null) field.SetValue(component, value);
                }
                catch (Exception ex)
                {
                    Debug.Error($"Failed to set field {kvp.Key}", ex);
                }
            }
        }

        /// <summary>
        /// Parses a property value stored for a component type.
        /// </summary>
        /// <param name="value">Serialized property value</param>
        /// <param name="targetType">Target type to deserialize to</param>
        /// <returns>Deserialized object value</returns>
        private static object? ParsePropertyValue(string value, Type targetType)
        {
            if (value == "null") return null;

            // Handle custom types
            if (targetType == typeof(AssetRef) || 
                (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(AssetRef<>)))
            {
                // Try to parse as new format "AssetRef:guid:type"
                if (value.StartsWith("(AssetRef:"))
                {
                    string[] parts = value[1..(value.Length - 1)].Split(':');
                    if (parts.Length >= 2)
                    {
                        string id = parts[1];
                        if (int.TryParse(parts[2], out int typeValue)) // Safely parse the enum value
                        {
                            AssetType expectedType = (AssetType)typeValue;

                            // Create instance based on type
                            if (targetType.IsGenericType) return Activator.CreateInstance(targetType, id); // For generic AssetRef<T>
                            else return Activator.CreateInstance(targetType, id, expectedType); // For non-generic AssetRef
                        }
                    }

                    // If parsing fails, log warning and return empty
                    Debug.Warning($"Failed to parse AssetRef format: {value}");
                }

                // Try to parse as raw ID (for backward compatibility with old saves)
                if (!string.IsNullOrEmpty(value) && !value.Contains('(') && !value.Contains(','))
                {
                    if (targetType.IsGenericType) return Activator.CreateInstance(targetType, value);
                    else return Activator.CreateInstance(targetType, value, AssetType.None);
                }

                // Return empty AssetRef as fallback
                if (targetType.IsGenericType) return Activator.CreateInstance(targetType, string.Empty);
                else return new AssetRef("", AssetType.None);
            }
            else if (targetType == typeof(float2))
            {
                // Parse format: "(1,2)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 2)
                {
                    return new float2(
                        float.Parse(parts[0]),
                        float.Parse(parts[1])
                    );
                }
            }
            else if (targetType == typeof(float3))
            {
                // Parse format: "(1,2,3)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 3)
                {
                    return new float3(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2])
                    );
                }
            }
            else if (targetType == typeof(float4))
            {
                // Parse format: "(1,2,3,4)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 4)
                {
                    return new float4(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                }
            }
            else if (targetType == typeof(float4x4))
            {
                // Parse format: "(1,0,0,0,0,1,0,0,0,0,1,0,0,0,-7,1)"
                string trimmed = value.Trim('(', ')');
                string[] parts = trimmed.Split(',');
                if (parts.Length == 16)
                {
                    return new float4x4(
                        float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]),
                        float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]),
                        float.Parse(parts[8]), float.Parse(parts[9]), float.Parse(parts[10]), float.Parse(parts[11]),
                        float.Parse(parts[12]), float.Parse(parts[13]), float.Parse(parts[14]), float.Parse(parts[15])
                    );
                }
            }
            else if (targetType == typeof(string))
            {
                // Remove quotes if still present
                if (value.StartsWith('"') && value.EndsWith('"')) return value.Trim('"');
                return value;
            }
            else if (targetType == typeof(bool)) return bool.Parse(value);
            else if (targetType == typeof(float)) return float.Parse(value);
            else if (targetType == typeof(int)) return int.Parse(value);
            else if (targetType == typeof(uint)) return uint.Parse(value);
            else if (targetType.IsEnum) return Enum.Parse(targetType, value);

            Debug.Warning($"Unhandled property parse type: {targetType.Name}");
            return null;
        }

        /// <summary>
        /// Determines if a field should be deserialized.
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <returns>True if the field should be deserialized</returns>
        private static bool ShouldDeserializeField(FieldInfo field)
        {
            if ((field.IsLiteral && !field.IsInitOnly) ||  // skip constants
                field.IsStatic || // skip static
                (field.IsInitOnly && !field.IsLiteral) || // skip readonly fields
                field.GetCustomAttribute<NonSerializedAttribute>() != null || // skip nonserialized
                field.GetCustomAttribute<CompilerGeneratedAttribute>() != null) return false; // skip hide in editor
            return true;
        }
    }
}
