using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents material properties of an entity.
    /// </summary>
    public class SDFMaterial : IComponent
    {
        public SDFMaterial()
        {
            albedoColor = ColorPalette.White;
            metallic = 0f;
            roughness = 1f;
        }

        [Color(false)] public float4 albedoColor;
        public float metallic;
        public float roughness;
    }
}
