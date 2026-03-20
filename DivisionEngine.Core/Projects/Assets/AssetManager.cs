//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Stores loaded assets and references.
    /// </summary>
    public class AssetManager
    {
        private readonly Dictionary<string, Asset> loadedAssets = [];
        private readonly Dictionary<string, int> referenceCounts = [];

        /// <summary>
        /// Gets an asset from a list of loaded assets.
        /// </summary>
        /// <param name="id">GUID of asset to find</param>
        /// <returns>Loaded asset base type</returns>
        public Asset? Get(string id) => loadedAssets.TryGetValue(id, out Asset? asset) ? asset : null;

        /// <summary>
        /// Gets an asset from a list of loaded assets.
        /// </summary>
        /// <typeparam name="T">Type of asset to find</typeparam>
        /// <param name="id">GUID of asset to find</param>
        /// <returns>Loaded asset</returns>
        public T? Get<T>(string id) where T : Asset => loadedAssets.TryGetValue(id, out Asset? asset) ? asset as T : null;

        /// <summary>
        /// Loads an asset asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of asset to load</typeparam>
        /// <param name="id">GUID of asset to load</param>
        /// <returns>Asset loading task of type <typeparamref name="T"/></returns>
        public async Task<T?> LoadAssetAsync<T>(string id) where T : Asset
        {
            if (loadedAssets.TryGetValue(id, out Asset? existing))
            {
                referenceCounts[id]++;
                return existing as T;
            }

            AssetMetadata? metadata = AssetDatabase.GetAssetMetadataByID(id);
            if (metadata == null) return null; // Null validation
            if (metadata.Type != AssetDatabase.GetAssetType<T>()) // Type validation
            {
                Debug.Error($"Asset type mismatch: Expected {AssetDatabase.GetAssetType<T>()}, got {metadata.Type}");
                return null;
            }

            Asset? asset = CreateAssetFromMetadata(metadata);
            if (asset == null) return null;
            if (!await asset.LoadAsync()) return null;

            Debug.Info($"Asset Manager: Loaded Asset:\n{metadata.FileName}");
            loadedAssets[id] = asset;
            referenceCounts[id] = 1;
            return asset as T;
        }

        /// <summary>
        /// Unloads an asset.
        /// </summary>
        /// <param name="id">GUID of asset to unload</param>
        public void UnloadAsset(string id)
        {
            if (!referenceCounts.TryGetValue(id, out int value)) return;
            referenceCounts[id] = --value;

            if (value <= 0)
            {
                if (loadedAssets.TryGetValue(id, out Asset? asset))
                {
                    asset.Unload();
                    Debug.Info($"Asset Manager: Unloaded Asset:\n{id}");
                    loadedAssets.Remove(id);
                }
                referenceCounts.Remove(id);
            }
        }

        /// <summary>
        /// Unloads all assets.
        /// </summary>
        public void UnloadAll()
        {
            Debug.Info($"Asset Manager: Unloaded Assets");
            foreach (Asset? asset in loadedAssets.Values) asset.Unload();
            loadedAssets.Clear();
            referenceCounts.Clear();
        }

        /// <summary>
        /// Creates an asset from metadata based on type.
        /// </summary>
        /// <param name="metadata">Loaded asset metadata</param>
        /// <returns>Asset instance of metadata type</returns>
        private static Asset? CreateAssetFromMetadata(AssetMetadata metadata) => metadata.Type switch
        {
            AssetType.Texture => new TextureAsset(metadata),
            AssetType.Material => new MaterialAsset(metadata),
            AssetType.Script => new ScriptAsset(metadata),
            AssetType.SDF => new SDFAsset(metadata),
            AssetType.Audio => new AudioAsset(metadata),
            // Add others as needed
            _ => null
        };
    }
}
