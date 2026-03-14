//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
//
namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Stores loaded assets and references.
    /// </summary>
    /// <param name="database">Asset database to pull from</param>
    public class AssetManager(AssetDatabase database)
    {
        private readonly AssetDatabase database = database;
        private readonly Dictionary<string, Asset> loadedAssets = [];
        private readonly Dictionary<string, int> referenceCounts = [];

        public Asset? Get(string id) => loadedAssets.TryGetValue(id, out Asset? asset) ? asset : null;

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

            AssetMetadata? metadata = database.GetAssetMetadataByID(id); // Now uncommented
            if (metadata == null) return null;

            // Add type validation
            if (metadata.Type != GetAssetType<T>())
            {
                Debug.Error($"Asset type mismatch: Expected {GetAssetType<T>()}, got {metadata.Type}");
                return null;
            }

            Asset? asset = CreateAssetFromMetadata(metadata);
            if (asset == null) return null;
            bool success = await asset.LoadAsync();
            if (!success) return null;

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
        private static Asset? CreateAssetFromMetadata(AssetMetadata metadata)
        {
            return metadata.Type switch
            {
                AssetType.Texture => new TextureAsset(metadata),
                AssetType.Material => new MaterialAsset(metadata),
                //AssetType.Script => new ScriptAsset(metadata), Add in future!
                //AssetType.SDF => new SDFLibraryAsset(metadata),
                // Add others as needed
                _ => null
            };
        }

        private static AssetType GetAssetType<T>() where T : Asset
        {
            if (typeof(T) == typeof(TextureAsset)) return AssetType.Texture;
            if (typeof(T) == typeof(MaterialAsset)) return AssetType.Material;
            // if (typeof(T) == typeof(SDFLibraryAsset)) return AssetType.SDF;
            // if (typeof(T) == typeof(ScriptAsset)) return AssetType.Script;
            if (typeof(T) == typeof(AudioAsset)) return AssetType.Audio;
            if (typeof(T) == typeof(FontAsset)) return AssetType.Font;
            return AssetType.None;
        }
    }
}
