using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

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
            absorptionColor = ColorPalette.White;
            maxRaySteps = 196;
        }

        public bool hasRefractions;
        [Color(ShowAlpha = false)] public float4 absorptionColor;
        public int maxRaySteps;

        public IComponent Clone() => new Refractions
        {
            maxRaySteps = maxRaySteps,
            absorptionColor = absorptionColor,
            hasRefractions = hasRefractions,
        };
    }
}
