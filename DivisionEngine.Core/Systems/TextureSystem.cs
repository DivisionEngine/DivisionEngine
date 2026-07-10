using DivisionEngine.Projects;
using DivisionEngine.Projects.Assets;
using DivisionEngine.Rendering.Textures;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Used for manipulating all project textures.
    /// </summary>
    public class TextureSystem : SystemBase
    {
        /// <summary>
        /// All texture data flattened into a single array (for GPU buffer).
        /// </summary>
        public static TextureData[] AllTextureData { get; private set; } = [];

        /// <summary>
        /// Metadata for each texture (resolution, offset in buffer).
        /// </summary>
        public static TextureMetadata[] AllTextureMetadata { get; private set; } = [];

        /// <summary>
        /// Dictionary mapping texture asset IDs to their metadata index.
        /// </summary>
        private static readonly Dictionary<string, int> textureIdToIndex = [];

        public override void Render()
        {
            // Create default sun light if no lights exits, to prevent a crash
            if (AllTextureMetadata.Length < 1 || AllTextureData.Length < 1)
            {
                AllTextureMetadata = [
                    new TextureMetadata
                    {
                        bufferOffset = 0,
                        resolution = 1,
                    }
                ];

                AllTextureData = [
                    new TextureData
                    {
                        pixel = new float4(1, 1, 1, 1),
                    }
                ];
            }
        }

        public override void Awake()
        {
            _ = LoadAllTexturesAsync();
        }

        /// <summary>
        /// Loads all textures from the asset database.
        /// </summary>
        public static async Task LoadAllTexturesAsync()
        {
            if (!ProjectManager.IsCurrentLoaded)
            {
                Debug.Info("TextureSystem: Cannot load textures without a current project loaded!");
                return;
            }

            // Get all texture assets from the database
            List<AssetMetadata> textureMetadatas = [.. AssetDatabase.GetAssetsByType(AssetType.Texture)];
            if (textureMetadatas.Count == 0)
            {
                Debug.Info("TextureSystem: No textures found in project");
                AllTextureData = [];
                AllTextureMetadata = [];
                return;
            }

            Debug.Info($"TextureSystem: Loading {textureMetadatas.Count} textures...");

            // Load each texture through the AssetManager (caches them)
            List<TextureAsset> loadedTextures = [];
            List<TextureMetadata> metadataList = [];
            int currentOffset = 0;

            foreach (AssetMetadata meta in textureMetadatas)
            {
                TextureAsset? texture = await ProjectManager.AssetManager!.LoadAssetAsync<TextureAsset>(meta.ID);
                if (texture == null || texture.PixelData == null)
                {
                    Debug.Warning($"TextureSystem: Failed to load texture: {meta.FileName}");
                    continue;
                }

                loadedTextures.Add(texture);

                // Add metadata
                metadataList.Add(new TextureMetadata
                {
                    resolution = new int2(texture.Width, texture.Height),
                    bufferOffset = currentOffset
                });

                textureIdToIndex[meta.ID] = loadedTextures.Count - 1;
                currentOffset += texture.PixelData.Length;
            }

            if (loadedTextures.Count == 0)
            {
                Debug.Warning("TextureSystem: No textures loaded successfully");
                AllTextureData = [];
                AllTextureMetadata = [];
                return;
            }

            // Flatten all texture data into a single array
            List<TextureData> allData = [];
            foreach (TextureAsset texture in loadedTextures)
            {
                if (texture.PixelData == null) continue;
                foreach (float4 pixel in texture.PixelData)
                    allData.Add(new TextureData { pixel = pixel });
            }

            AllTextureData = [.. allData];
            AllTextureMetadata = [.. metadataList];

            Debug.Info($"TextureSystem: Loaded {loadedTextures.Count} textures, {AllTextureData.Length} total pixels");
        }

        /// <summary>
        /// Gets texture metadata by asset ID.
        /// </summary>
        public static TextureMetadata? GetTextureMetadata(string assetId)
        {
            if (textureIdToIndex.TryGetValue(assetId, out int index))
            {
                return AllTextureMetadata[index];
            }
            return null;
        }

        /// <summary>
        /// Gets a texture's pixel data by asset ID.
        /// </summary>
        public static TextureData[]? GetTextureData(string assetId)
        {
            if (textureIdToIndex.TryGetValue(assetId, out int index))
            {
                var meta = AllTextureMetadata[index];
                int start = meta.bufferOffset;
                int length = meta.resolution.X * meta.resolution.Y;
                var result = new TextureData[length];
                Array.Copy(AllTextureData, start, result, 0, length);
                return result;
            }
            return null;
        }

        /// <summary>
        /// Unloads all textures from the system.
        /// </summary>
        public static void UnloadAll()
        {
            AllTextureData = [];
            AllTextureMetadata = [];
            textureIdToIndex.Clear();
            Debug.Info("TextureSystem: Unloaded all textures");
        }
    }
}
