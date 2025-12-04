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
    }
}
