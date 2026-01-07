using DivisionEngine.Components.FieldAttributes;
using DivisionEngine.MathLib;

namespace DivisionEngine.Components.SDFs
{
    /// <summary>
    /// Represents material properties of an entity.
    /// </summary>
    public class SDFMaterial : IComponent
    {
        /// <summary>
        /// White material with metallic = 0, roughness = 0.5, specular = 0.5, and ior = 1.
        /// </summary>
        public SDFMaterial()
        {
            albedoColor = ColorPalette.White;
            metallic = 0f;
            roughness = 0.5f;
            specular = 0.5f;
            ior = 1.0f;
        }

        [Color(false)] public float4 albedoColor;
        public float metallic;
        public float roughness;
        public float specular;
        public float ior;
    }
}
