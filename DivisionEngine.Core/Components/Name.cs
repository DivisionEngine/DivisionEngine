namespace DivisionEngine.Components
{
    /// <summary>
    /// Special tag component allowing the naming of entities.
    /// </summary>
    public class Name : IComponent
    {
        /// <summary>
        /// Creates a new null name component.
        /// </summary>
        public Name()
        {
            name = null;
        }

        /// <summary>
        /// Builds a name component with name.
        /// </summary>
        /// <param name="name">Name to set component to</param>
        public Name(string name)
        {
            this.name = name;
        }

        public string? name;
    }
}
