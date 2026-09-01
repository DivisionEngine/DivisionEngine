//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using DivisionEngine.Projects;
using DivisionEngine.Projects.Assets;
using DivisionEngine.Rendering;
using Silk.NET.Vulkan;
using static System.Net.Mime.MediaTypeNames;
using Math = DivisionEngine.MathLib.Math;

namespace DivisionEngine.Systems
{
    /// <summary>
    /// Used for manipulating all project textures.
    /// </summary>
    public class TextureSystem : SystemBase
    {
        private static uint[]? _allTextureData = [];
        private static TextureMetadata[]? _allTextureMetadata = [];

        /// <summary>
        /// All texture data flattened into a single array (for GPU buffer).
        /// </summary>
        public static uint[]? AllTextureData
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
        /// How many textures were loaded on latest texture buffer rebuild.
        /// </summary>
        public static int LastLoadedTextureCount { get; private set; } = 0;

        /// <summary>
        /// The texture buffer size on the latest texture buffer rebuild.
        /// </summary>
        public static int LastLoadedTextureBufferSize { get; private set; } = 0;

        /// <summary>
        /// Use this to determine when the texture data has fully loaded or been modified.
        /// </summary>
        public static event Action? UpdatedTextureData;

        /// <summary>
        /// Called when texture data is called to load.
        /// </summary>
        public static event Action? StartedLoadingTextureData;

        /// <summary>
        /// 0.0 - 1.0, the progress of the texture loader when active.
        /// </summary>
        public static float TextureLoadProgress { get; private set; } = 0f;

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
                        mipCount = 1,
                    }
                ];
                AllTextureData = [0];
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
            StartedLoadingTextureData?.Invoke();
            TextureLoadProgress = 0f;
            loadingTextures = true;

            if (!ProjectManager.IsCurrentLoaded)
            {
                Debug.Info("Texture System: Cannot load textures without a current project loaded!");
                UpdatedTextureData?.Invoke();
                loadingTextures = false;
                return;
            }

            // Get all texture assets from the database
            List<AssetMetadata> textureMetadatas = [.. AssetDatabase.GetAssetsByType(AssetType.Texture)];
            if (textureMetadatas.Count == 0)
            {
                Debug.Info("Texture System: No textures found in project");
                AllTextureData = [];
                AllTextureMetadata = [];
                LastLoadedTextureCount = 0;
                LastLoadedTextureBufferSize = 0;
                UpdatedTextureData?.Invoke();
                loadingTextures = false;
                return;
            }

            Debug.Info($"Texture System: Loading {textureMetadatas.Count} textures...");

            // Load each texture through the AssetManager (caches them)
            List<TextureAsset> loadedTextures = [];
            List<TextureMetadata> metadataList = [];
            List<uint> allData = [];
            int currentOffset = 0;
            int completedMips = 0;
            foreach (AssetMetadata meta in textureMetadatas)
            {
                TextureAsset? texture = await ProjectManager.AssetManager!.LoadAssetAsync<TextureAsset>(meta.ID);
                if (texture == null)
                {
                    Debug.Warning($"Texture System: Failed to load texture: {meta.FileName}");
                    continue;
                }

                loadedTextures.Add(texture);
                if (texture?.PixelData == null) continue;

                int maxLevels = (int)Math.Floor(Math.Log2(Math.Max(texture.Width, texture.Height))) + 1;
                List<uint[]> mipChain = await BuildMipChainAsync(texture.PixelData!, texture.Width, texture.Height,
                    (progressIncrement) =>
                    {
                        // This callback is called after each mip level is generated.
                        completedMips += progressIncrement;
                        TextureLoadProgress += 1f / maxLevels / textureMetadatas.Count;
                    });

                metadataList.Add(new TextureMetadata
                {
                    resolution = new int2(texture.Width, texture.Height),
                    bufferOffset = currentOffset,
                    mipCount = mipChain.Count,
                });
                textureIdToIndex[meta.ID] = metadataList.Count - 1;

                foreach (uint[] level in mipChain)
                {
                    foreach (uint pixel in level) allData.Add(pixel);
                    currentOffset += level.Length;
                }

                GC.Collect(); // Collect GC after every texture loaded
                TextureLoadProgress = (float)loadedTextures.Count / textureMetadatas.Count; // set to make sure progress doesn't over/under shoot
            }

            if (loadedTextures.Count == 0)
            {
                Debug.Warning("Texture System: No textures loaded successfully");
                AllTextureData = [];
                AllTextureMetadata = [];
                LastLoadedTextureCount = 0;
                LastLoadedTextureBufferSize = 0;
                UpdatedTextureData?.Invoke();
                loadingTextures = false;
                TextureLoadProgress = 0f;
                return;
            }

            AllTextureData = [.. allData];
            AllTextureMetadata = [.. metadataList];
            LastLoadedTextureCount = AllTextureMetadata.Length;
            LastLoadedTextureBufferSize = AllTextureData.Length;

            Debug.Info($"Texture System: Loaded {loadedTextures.Count} textures, {AllTextureData.Length} total pixels");
            UpdatedTextureData?.Invoke();
            loadingTextures = false;
            TextureLoadProgress = 1f;
        }

        private static async Task<List<uint[]>> BuildMipChainAsync(uint[] baseLevel, int width, int height, Action<int> onProgress)
        {
            List<uint[]> levels = [baseLevel];
            int w = width, h = height;
            uint[] prev = baseLevel;
            int maxLevels = (int)Math.Floor(Math.Log2(Math.Max(width, height))) + 1;
            int generated = 1; // base level already done
            onProgress(1); // report base level

            while ((w > 1 || h > 1) && levels.Count < maxLevels)
            {
                int nw = Math.Max(1, w / 2);
                int nh = Math.Max(1, h / 2);
                uint[] next = new uint[nw * nh];

                // Offload the averaging to a background thread
                await Task.Run(() =>
                {
                    for (int y = 0; y < nh; y++)
                    {
                        for (int x = 0; x < nw; x++)
                        {
                            int x0 = Math.Min(x * 2, w - 1);
                            int x1 = Math.Min(x * 2 + 1, w - 1);
                            int y0 = Math.Min(y * 2, h - 1);
                            int y1 = Math.Min(y * 2 + 1, h - 1);
                            next[y * nw + x] = AveragePixels(
                                prev[y0 * w + x0], prev[y0 * w + x1],
                                prev[y1 * w + x0], prev[y1 * w + x1]);
                        }
                    }
                });

                levels.Add(next);
                prev = next;
                w = nw; h = nh;
                generated++;
                onProgress(1); // report one mip level done
            }

            return levels;
        }

        private static uint AveragePixels(uint a, uint b, uint c, uint d)
        {
            int r = (UnpackChannel(a, 0) + UnpackChannel(b, 0) + UnpackChannel(c, 0) + UnpackChannel(d, 0)) / 4;
            int g = (UnpackChannel(a, 1) + UnpackChannel(b, 1) + UnpackChannel(c, 1) + UnpackChannel(d, 1)) / 4;
            int b_ = (UnpackChannel(a, 2) + UnpackChannel(b, 2) + UnpackChannel(c, 2) + UnpackChannel(d, 2)) / 4;
            int al = (UnpackChannel(a, 3) + UnpackChannel(b, 3) + UnpackChannel(c, 3) + UnpackChannel(d, 3)) / 4;
            return (uint)((r << 24) | (g << 16) | (b_ << 8) | al);
        }

        private static int UnpackChannel(uint packed, int channel)
        {
            int shift = channel switch { 0 => 24, 1 => 16, 2 => 8, 3 => 0, _ => 0 };
            return (int)((packed >> shift) & 0xFF);
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
        public static uint[]? GetTextureData(string assetId)
        {
            if (textureIdToIndex.TryGetValue(assetId, out int index))
            {
                if (AllTextureMetadata == null || AllTextureData == null) return null;
                TextureMetadata meta = AllTextureMetadata[index];
                int start = meta.bufferOffset;
                int length = meta.resolution.X * meta.resolution.Y;
                uint[] result = new uint[length];
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
            Debug.Info("Texture System: Unloaded all textures");
        }

        public static void FreeCPUTextureData()
        {
            _allTextureData = null;
            _allTextureMetadata = null;
            GC.Collect(); // Force garbage collection
            Debug.Info("Texture System: Freed CPU texture data");
        }
    }
}
