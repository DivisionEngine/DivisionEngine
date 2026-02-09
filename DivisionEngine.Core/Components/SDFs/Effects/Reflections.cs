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
            hasReflections = true;
            maxBounces = 2;
        }

        public bool hasReflections;
        [Range(1, 16)] public int maxBounces;

        public IComponent Clone() => new Reflections
        {
            hasReflections = hasReflections,
            maxBounces = maxBounces,
        };
    }
}
