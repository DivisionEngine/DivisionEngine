namespace DivisionEngine.Components.FieldAttributes
{

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {

        public float Minimum { get; set; } = 0f;
        public float Maximum { get; set; } = 1f;

        public RangeAttribute() { }
    }
}
