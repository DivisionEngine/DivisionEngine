using DivisionEngine.MathLib;

namespace DivisionEngine.Components.Lights
{
    /// <summary>
    /// Represents a directional light in the world.
    /// </summary>
    public class DirectionalLight : IComponent
    {
        public DirectionalLight()
        {
            color = ColorPalette.White;
            direction = new float3(1f, 0f, 0f);
            intensity = 1f;
        }

        public float4 color;
        public float3 direction;
        public float intensity;
    }
}
