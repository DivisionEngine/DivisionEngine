using DivisionEngine.Components.FieldAttributes;

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
            absorptionColor = new float4(1f, 1f, 1f, 0.1f);
            maxRaySteps = 196;
            maxRecursionTraces = 4;
        }

        public bool hasRefractions;
        [Color(ShowAlpha = true)] public float4 absorptionColor;
        public int maxRaySteps;
        [Range(1, 16)] public int maxRecursionTraces;

        public IComponent Clone() => new Refractions
        {
            maxRaySteps = maxRaySteps,
            absorptionColor = absorptionColor,
            hasRefractions = hasRefractions,
            maxRecursionTraces = maxRecursionTraces,
        };
    }
}
