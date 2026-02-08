using DivisionEngine.Components.FieldAttributes;

namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to receive reflections.
    /// </summary>
    public class Reflections : IComponent
    {
        public Reflections()
        {
            maxBounces = 2;
        }

        [Range(1, 16)] public int maxBounces;

        public IComponent Clone() => new Reflections
        {
            maxBounces = maxBounces,
        };
    }
}
