namespace DivisionEngine.Components.FieldAttributes
{
    /// <summary>
    /// Used for specifying float4 fields as quaternions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class RotationAttribute : Attribute
    {
        /// <summary>
        /// Creates a new RotationAttribute with default settings.
        /// </summary>
        public RotationAttribute() { }
    }
}
