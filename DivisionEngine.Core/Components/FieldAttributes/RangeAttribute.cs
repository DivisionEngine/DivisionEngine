namespace DivisionEngine.Components.FieldAttributes
{
    /// <summary>
    /// Used for specifying integer and float fields as sliders.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        /// <summary>
        /// Minimum slider value.
        /// </summary>
        public float Min { get; }

        /// <summary>
        /// Maximum slider value.
        /// </summary>
        public float Max { get; }

        /// <summary>
        /// Applies a range slider.
        /// </summary>
        /// <param name="min">Minimum slider value</param>
        /// <param name="max">Maximum slider value</param>
        public RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
