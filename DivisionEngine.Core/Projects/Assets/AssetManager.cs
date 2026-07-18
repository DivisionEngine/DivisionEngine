//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects.Assets
{
    public enum AssetLoadState { Unloaded, Loading, Loaded }

    public class AssetManager
    {
        private readonly Dictionary<string, Asset> loadedAssets = [];
        private readonly Dictionary<string, int> referenceCounts = [];
        private readonly Dictionary<string, AssetLoadState> loadStates = [];
        private readonly Dictionary<string, Task> inFlightLoads = [];
        private readonly Lock stateLock = new();

        /// <summary>
        /// Fired whenever an asset's load state changes (Unloaded/Loading/Loaded).
        /// May be invoked from a background thread — subscribers must marshal to UI thread.
        /// </summary>
        public event Action<string, AssetLoadState>? AssetLoadStateChanged;

        public Asset? Get(string id) => loadedAssets.TryGetValue(id, out Asset? asset) ? asset : null;
        public T? Get<T>(string id) where T : Asset => loadedAssets.TryGetValue(id, out Asset? asset) ? asset as T : null;

        public AssetLoadState GetLoadState(string id) =>
            loadStates.TryGetValue(id, out AssetLoadState state) ? state : AssetLoadState.Unloaded;

        public async Task<T?> LoadAssetAsync<T>(string id) where T : Asset
        {
            lock (stateLock)
            {
                if (loadedAssets.TryGetValue(id, out Asset? existing))
                {
                    referenceCounts[id]++;
                    return existing as T;
                }
            }

            // A concurrent call (e.g. two components referencing the same asset,
            // or a watcher-triggered reload racing a manual load) may already be
            // loading this ID — piggyback on it instead of loading it twice.
            Task? existingLoad;
            lock (stateLock) inFlightLoads.TryGetValue(id, out existingLoad);
            if (existingLoad != null)
            {
                await existingLoad;
                lock (stateLock)
                {
                    if (loadedAssets.TryGetValue(id, out Asset? loaded))
                    {
                        referenceCounts[id]++;
                        return loaded as T;
                    }
                }
                return null;
            }

            AssetMetadata? metadata = AssetDatabase.GetAssetMetadataByID(id);
            if (metadata == null) return null;
            if (metadata.Type != AssetDatabase.GetAssetType<T>())
            {
                Debug.Error($"Asset type mismatch: Expected {AssetDatabase.GetAssetType<T>()}, got {metadata.Type}");
                return null;
            }

            Asset? asset = CreateAssetFromMetadata(metadata);
            if (asset == null) return null;

            TaskCompletionSource loadTcs = new();
            lock (stateLock) inFlightLoads[id] = loadTcs.Task;
            SetLoadState(id, AssetLoadState.Loading);

            try
            {
                bool success = await asset.LoadAsync();
                if (!success)
                {
                    SetLoadState(id, AssetLoadState.Unloaded);
                    return null;
                }

                Debug.Info($"Asset Manager: Loaded Asset:\n{metadata.FileName}");
                lock (stateLock)
                {
                    loadedAssets[id] = asset;
                    referenceCounts[id] = 1;
                }
                SetLoadState(id, AssetLoadState.Loaded);
                return asset as T;
            }
            finally
            {
                lock (stateLock) inFlightLoads.Remove(id);
                loadTcs.SetResult();
            }
        }

        public void UnloadAsset(string id)
        {
            bool shouldUnload = false;
            Asset? assetToUnload = null;

            lock (stateLock)
            {
                if (!referenceCounts.TryGetValue(id, out int value)) return;
                referenceCounts[id] = --value;

                if (value <= 0)
                {
                    loadedAssets.TryGetValue(id, out assetToUnload);
                    loadedAssets.Remove(id);
                    referenceCounts.Remove(id);
                    shouldUnload = true;
                }
            }

            if (shouldUnload)
            {
                assetToUnload?.Unload();
                Debug.Info($"Asset Manager: Unloaded Asset:\n{id}");
                SetLoadState(id, AssetLoadState.Unloaded);
            }
        }

        public void UnloadAll()
        {
            Debug.Info($"Asset Manager: Unloaded Assets");
            List<string> ids;
            lock (stateLock) ids = [.. loadedAssets.Keys];

            foreach (string id in ids)
            {
                loadedAssets[id].Unload();
                SetLoadState(id, AssetLoadState.Unloaded);
            }

            lock (stateLock)
            {
                loadedAssets.Clear();
                referenceCounts.Clear();
            }
        }

        private void SetLoadState(string id, AssetLoadState state)
        {
            lock (stateLock) loadStates[id] = state;
            AssetLoadStateChanged?.Invoke(id, state);
        }

        private static Asset? CreateAssetFromMetadata(AssetMetadata metadata) => metadata.Type switch
        {
            AssetType.Texture => new TextureAsset(metadata),
            AssetType.Material => new MaterialAsset(metadata),
            AssetType.Script => new ScriptAsset(metadata),
            AssetType.SDF => new SDFAsset(metadata),
            AssetType.Audio => new AudioAsset(metadata),
            _ => null
        };
    }
}
