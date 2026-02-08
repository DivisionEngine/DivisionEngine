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
        }

        [Color(true)] public float4 backgroundColor;

        public IComponent Clone() => new Environment
        {
            backgroundColor = backgroundColor,
        };
    }
}
