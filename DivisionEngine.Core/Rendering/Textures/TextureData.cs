//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
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
