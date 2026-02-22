namespace DivisionEngine.Projects.Assets
{

    public class AssetManager
    {
        private readonly AssetDatabase _database;
        private readonly Dictionary<string, Asset> _loadedAssets = [];
        private readonly Dictionary<string, int> _referenceCounts = [];

        public AssetManager(AssetDatabase database)
        {
            _database = database;
        }

        public async Task<T?> LoadAssetAsync<T>(string guid) where T : Asset
        {
            // Check if already loaded
            if (_loadedAssets.TryGetValue(guid, out var existing))
            {
                _referenceCounts[guid]++;
                return existing as T;
            }

            // Get metadata
            var metadata = _database.GetAssetByGuid(guid);
            if (metadata == null) return null;

            // Create appropriate asset type
            var asset = CreateAssetFromMetadata(metadata);
            if (asset == null) return null;

            // Load the asset
            bool success = await asset.LoadAsync();
            if (!success) return null;

            // Store in cache
            _loadedAssets[guid] = asset;
            _referenceCounts[guid] = 1;

            return asset as T;
        }

        public void UnloadAsset(string guid)
        {
            if (!_referenceCounts.TryGetValue(guid, out int value)) return;

            _referenceCounts[guid] = --value;

            if (value <= 0)
            {
                if (_loadedAssets.TryGetValue(guid, out var asset))
                {
                    asset.Unload();
                    _loadedAssets.Remove(guid);
                }
                _referenceCounts.Remove(guid);
            }
        }

        public void UnloadAll()
        {
            foreach (var asset in _loadedAssets.Values)
            {
                asset.Unload();
            }
            _loadedAssets.Clear();
            _referenceCounts.Clear();
        }

        private Asset? CreateAssetFromMetadata(AssetMetadata metadata)
        {
            return metadata.Type switch
            {
                //AssetType.Texture => new TextureAsset(metadata),
                //AssetType.Material => new MaterialAsset(metadata),
                //AssetType.Script => new ScriptAsset(metadata),
                //AssetType.SDF => new SDFLibraryAsset(metadata),
                // Add others as needed
                _ => null
            };
        }
    }
}
