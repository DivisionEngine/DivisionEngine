namespace DivisionEngine.Settings
{
    /// <summary>
    /// Represents a generic serializable settings object.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Identifier of the settings object as a whole.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Dictionary of settings values.
        /// </summary>
        public Dictionary<string, object> Settings { get; }

        /// <summary>
        /// Called after loading to validate or initialize settings.
        /// </summary>
        public virtual void OnLoad() { }

        /// <summary>
        /// Called before saving.
        /// </summary>
        public virtual void OnSave() { }
    }
}
