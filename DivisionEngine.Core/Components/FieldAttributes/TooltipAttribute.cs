namespace DivisionEngine.Components.FieldAttributes
{
    /// <summary>
    /// Used for specifying tooltips on fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class TooltipAttribute : Attribute
    {
        /// <summary>
        /// Tooltip text.
        /// </summary>
        public string Tooltip { get; }

        /// <summary>
        /// Applies a tooltip to field.
        /// </summary>
        /// <param name="tooltip">Tooltip text</param>
        public TooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}
