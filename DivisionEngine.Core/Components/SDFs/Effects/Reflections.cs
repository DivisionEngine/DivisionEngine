namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to receive reflections.
    /// </summary>
    public class Reflections : IComponent
    {
        public Reflections()
        {
            maxRaySteps = 64;
        }

        public int maxRaySteps;
    }
}
