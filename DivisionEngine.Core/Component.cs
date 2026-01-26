namespace DivisionEngine
{
    /// <summary>
    /// Base interface for defining components.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Creates a deep copy of the component.
        /// </summary>
        /// <returns>A new instance with the same values</returns>
        IComponent Clone();
    }
}
