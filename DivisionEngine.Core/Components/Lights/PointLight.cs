using DivisionEngine.MathLib;

namespace DivisionEngine.Components.Lights
{
    /// <summary>
    /// Represents a point light in the world.
    /// </summary>
    public class PointLight : IComponent
    {
        public PointLight()
        {
            color = ColorPalette.White;
            intensity = 1f;
            radius = 10f;
        }

        public float4 color;
        public float intensity;
        public float radius;
    }
}
