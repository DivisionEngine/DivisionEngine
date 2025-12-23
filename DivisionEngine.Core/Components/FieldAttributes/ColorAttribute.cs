namespace DivisionEngine.Components.FieldAttributes
{
    /// <summary>
    /// Used for specifying float4 fields as colors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class ColorAttribute : Attribute
    {
        /// <summary>
        /// Optional color space (RGBA, HSV, etc).
        /// </summary>
        public string ColorSpace { get; set; } = "RGBA";

        /// <summary>
        /// Whether to show alpha channel.
        /// </summary>
        public bool ShowAlpha { get; set; } = true;

        /// <summary>
        /// Whether to show the color picker as HDR color.
        /// </summary>
        public bool HDR { get; set; } = false;

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
