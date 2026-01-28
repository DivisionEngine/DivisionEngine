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
        /// White material with metallic = 0, roughness = 0.5, specular = 0.5, ior = 1, and ao = 0.
        /// </summary>
        public SDFMaterial()
        {
            albedoColor = ColorPalette.White;
            metallic = 0f;
            roughness = 0.5f;
            specular = 0.5f;
            indexOfRefraction = 1.0f;
            ambientOcclusion = 0.3f;
        }

        [Color(false)] public float4 albedoColor;
        [Range(0f, 1f)] public float metallic;
        [Range(0f, 1f)] public float roughness;
        public float specular;
        public float indexOfRefraction;
        [Range(0f, 1f)] public float ambientOcclusion;

        public IComponent Clone() => new SDFMaterial
        {
            albedoColor = albedoColor,
            metallic = metallic,
            roughness = roughness,
            specular = specular,
            indexOfRefraction = indexOfRefraction,
            ambientOcclusion = ambientOcclusion,
        };
    }
}
