namespace DivisionEngine.Components
{
    /// <summary>
    /// Special tag component allowing the naming of entities.
    /// </summary>
    public class Name : IComponent
    {
        public Name()
        {
            name = null;
        }

        public Name(string name)
        {
            this.name = name;
        }

        public string? name;
    }
}
