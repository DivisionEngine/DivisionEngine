namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Used for marking objects as serializable for hard state management.
    /// </summary>
    public interface ISerializable
    {
        /// <summary>
        /// Serializes an object.
        /// </summary>
        /// <returns>Serialized json structure</returns>
        string Serialize();

        /// <summary>
        /// Deseralizes an object.
        /// </summary>
        void Deserialize(string obj);
    }
}
