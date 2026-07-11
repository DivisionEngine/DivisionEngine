//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
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
        private static TextureData[]? _allTextureData = [];
        private static TextureMetadata[]? _allTextureMetadata = [];

        /// <summary>
        /// All texture data flattened into a single array (for GPU buffer).
        /// </summary>
        public static TextureData[]? AllTextureData
        {
            get => _allTextureData;
            private set => _allTextureData = value;
        }

        /// <summary>
        /// Metadata for each texture (resolution, offset in buffer).
        /// </summary>
        public static TextureMetadata[]? AllTextureMetadata
        {
            get => _allTextureMetadata;
            private set => _allTextureMetadata = value;
        }

        /// <summary>
        /// Use this to determine when the texture data is changed.
        /// </summary>
        public static event Action? UpdatedTextureData;

        /// <summary>
        /// Dictionary mapping texture asset IDs to their metadata index.
        /// </summary>
        private static readonly Dictionary<string, int> textureIdToIndex = [];

        private static bool mustReloadTextures = false;
        private static bool loadingTextures = false;

        public override void Render()
        {
            if (mustReloadTextures && !loadingTextures)
            {
                _ = LoadAllTexturesAsync();
                mustReloadTextures = false;
            }

            // Create default texture if none exist to prevent a crash
            if (AllTextureMetadata == null || AllTextureData == null) return;
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
                        packedPixel = 0,
                    }
                ];
            }
        }

        public override void AppStart()
        {
            loadingTextures = false;
            mustReloadTextures = false;
            AssetDatabase.AssetsUpdated += () => mustReloadTextures = true;
            ProjectManager.ProjectLoaded += () => mustReloadTextures = true;
            ProjectManager.ProjectClosed += () => mustReloadTextures = true;
        }

        public override void Awake()
        {
            mustReloadTextures = true;
        }

        /// <summary>
        /// Loads all textures from the asset database.
        /// </summary>
        public static async Task LoadAllTexturesAsync()
        {
            if (loadingTextures) return;
            loadingTextures = true;

            if (!ProjectManager.IsCurrentLoaded)
            {
                Debug.Info("TextureSystem: Cannot load textures without a current project loaded!");
                UpdatedTextureData?.Invoke();
                loadingTextures = false;
                return;
            }

            // Get all texture assets from the database
            List<AssetMetadata> textureMetadatas = [.. AssetDatabase.GetAssetsByType(AssetType.Texture)];
            if (textureMetadatas.Count == 0)
            {
                Debug.Info("TextureSystem: No textures found in project");
                AllTextureData = [];
                AllTextureMetadata = [];
                UpdatedTextureData?.Invoke();
                loadingTextures = false;
                return;
            }

            Debug.Info($"TextureSystem: Loading {textureMetadatas.Count} textures...");

            // Load each texture through the AssetManager (caches them)
            List<TextureAsset> loadedTextures = [];
            List<TextureMetadata> metadataList = [];
            List<TextureData> allData = [];
            int currentOffset = 0;
            foreach (AssetMetadata meta in textureMetadatas)
            {
                TextureAsset? texture = await ProjectManager.AssetManager!.LoadAssetAsync<TextureAsset>(meta.ID);
                if (texture == null)
                {
                    Debug.Warning($"TextureSystem: Failed to load texture: {meta.FileName}");
                    continue;
                }

                // Set loaded textures and metadata
                loadedTextures.Add(texture);
                metadataList.Add(new TextureMetadata
                {
                    resolution = new int2(texture.Width, texture.Height),
                    bufferOffset = currentOffset
                });
                textureIdToIndex[meta.ID] = loadedTextures.Count - 1;

                // Set pixel data
                if (texture?.PixelData == null) continue;
                foreach (uint pixel in texture.PixelData)
                    allData.Add(new TextureData { packedPixel = pixel });

                currentOffset += texture.PixelData.Length;
                GC.Collect(); // Collect GC after every texture loaded
            }

            if (loadedTextures.Count == 0)
            {
                Debug.Warning("TextureSystem: No textures loaded successfully");
                AllTextureData = [];
                AllTextureMetadata = [];
                UpdatedTextureData?.Invoke();
                loadingTextures = false;
                return;
            }

            AllTextureData = [.. allData];
            AllTextureMetadata = [.. metadataList];

            Debug.Info($"TextureSystem: Loaded {loadedTextures.Count} textures, {AllTextureData.Length} total pixels");
            UpdatedTextureData?.Invoke();
            loadingTextures = false;
        }

        /// <summary>
        /// Get the index that the metadata of the texture is stored at (for rendering).
        /// </summary>
        /// <param name="assetId">Asset ID of the texture</param>
        /// <returns>Metadata array index</returns>
        public static int GetTextureMetadataIndex(string assetId)
        {
            if (textureIdToIndex.TryGetValue(assetId, out int index)) return index;
            return -1;
        }

        /// <summary>
        /// Gets texture metadata by asset ID.
        /// </summary>
        public static TextureMetadata? GetTextureMetadata(string assetId)
        {
            if (textureIdToIndex.TryGetValue(assetId, out int index)) return AllTextureMetadata?[index];
            return null;
        }

        /// <summary>
        /// Gets a texture's pixel data by asset ID.
        /// </summary>
        public static TextureData[]? GetTextureData(string assetId)
        {
            if (textureIdToIndex.TryGetValue(assetId, out int index))
            {
                if (AllTextureMetadata == null || AllTextureData == null) return null;
                TextureMetadata meta = AllTextureMetadata[index];
                int start = meta.bufferOffset;
                int length = meta.resolution.X * meta.resolution.Y;
                TextureData[] result = new TextureData[length];
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

        public static void FreeCPUTextureData()
        {
            _allTextureData = null;
            _allTextureMetadata = null;
            GC.Collect(); // Force garbage collection
            Debug.Info("TextureSystem: Freed CPU texture data");
        }
    }
}
