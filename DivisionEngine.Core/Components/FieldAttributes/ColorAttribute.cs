namespace DivisionEngine.Components.FieldAttributes
{
    /// <summary>
    /// Used for specifying float4 fields as colors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ColorAttribute : Attribute
    {
        /// <summary>
        /// Whether to show alpha channel.
        /// </summary>
        public bool ShowAlpha { get; set; } = true;

        /// <summary>
        /// Creates a new ColorAttribute with default settings.
        /// </summary>
        public ColorAttribute() { }

        /// <summary>
        /// Creates a new ColorAttribute with specified alpha visibility.
        /// </summary>
        /// <param name="showAlpha">Whether to show alpha channel</param>
        public ColorAttribute(bool showAlpha)
        {
            ShowAlpha = showAlpha;
        }
    }
}
