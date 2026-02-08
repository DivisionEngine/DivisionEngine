using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents the world environment.
    /// </summary>
    public class Environment : IComponent
    {
        /// <summary>
        /// Environment with basic blue sky.
        /// </summary>
        public Environment()
        {
            backgroundColor = ColorPalette.SkyBlue;
            divisionDenoiseThreshold = 0.15f;
        }

        [Color(true)] public float4 backgroundColor;
        [Range(0f, 1f)] public float divisionDenoiseThreshold;

        public IComponent Clone() => new Environment
        {
            backgroundColor = backgroundColor,
            divisionDenoiseThreshold = divisionDenoiseThreshold,
        };
    }
}
