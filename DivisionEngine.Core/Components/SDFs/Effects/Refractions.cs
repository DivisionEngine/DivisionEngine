namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to receive refractions.
    /// </summary>
    public class Refractions : IComponent
    {
        public Refractions()
        {
            hasRefractions = true;
            maxRaySteps = 196;
        }

        public bool hasRefractions;
        public int maxRaySteps;

        public IComponent Clone() => new Refractions
        {
            maxRaySteps = maxRaySteps,
            hasRefractions = hasRefractions,
        };
    }
}
