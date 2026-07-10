namespace DivisionEngine.Rendering.Textures
{
     /// <summary>
     /// Stores data for a single pixel.
     /// </summary>
    public struct TextureData
    {
        public float4 pixel;
    }

    /// <summary>
    /// Metadata for a loaded texture.
    /// </summary>
    public struct TextureMetadata
    {
        public int2 resolution;  // Width, Height
        public int bufferOffset;  // Starting index in the big buffer
    }
}
