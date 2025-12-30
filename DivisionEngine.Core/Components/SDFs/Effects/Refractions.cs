namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to receive refractions.
    /// </summary>
    public class Refractions : IComponent
    {
        public Refractions()
        {
            maxRaySteps = 64;
            indexOfRefraction = 1.333f;
        }

        public int maxRaySteps;
        public float indexOfRefraction;
    }
}
