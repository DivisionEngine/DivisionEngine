//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
using System.Text.Json.Serialization;

namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Type of an asset.
    /// </summary>
    public enum AssetType
    {
        None, Texture, Script, SDF, Material, Audio, Font,
    }

    /// <summary>
    /// The base class of all asset types, not for direct use.
    /// </summary>
    /// <param name="metadata">Asset metadata</param>
    public abstract class Asset(AssetMetadata metadata)
    {
        public string ID { get; protected set; } = metadata.ID;
        public string RelativePath { get; protected set; } = metadata.RelativePath;
        public AssetMetadata Metadata { get; protected set; } = metadata;
        public bool IsLoaded { get; protected set; }

        public abstract Task<bool> LoadAsync();
        public abstract void Unload();
    }

    /// <summary>
    /// Reference to an asset. This is what components store.
    /// </summary>
    public struct AssetRef(string id, AssetType type)
    {
        public string ID { get; set; } = id;
        public AssetType ExpectedType { get; set; } = type;

        public AssetRef() : this(string.Empty, AssetType.None) { }

        [JsonIgnore] public Asset? LoadedAsset { get; internal set; } = null;
        [JsonIgnore] public readonly bool IsLoaded => LoadedAsset != null;

        public readonly bool IsValid() => !string.IsNullOrEmpty(ID);
    }

    /// <summary>
    /// Reference to an asset. This is what components store.
    /// </summary>
    public struct AssetRef<T>(string id) where T : Asset
    {
        public string ID { get; set; } = id;
        public AssetType ExpectedType { get; set; } = AssetDatabase.GetAssetType<T>();

        public AssetRef() : this(string.Empty) { }

        [JsonIgnore] public T? LoadedAsset { get; internal set; } = null;
        [JsonIgnore] public readonly bool IsLoaded => LoadedAsset != null;

        public static implicit operator AssetRef(AssetRef<T> generic) =>
            new(generic.ID, generic.ExpectedType);

        public static implicit operator AssetRef<T>(AssetRef standard)
        {
            if (standard.ExpectedType != AssetDatabase.GetAssetType<T>())
                throw new InvalidCastException($"Cannot cast AssetRef of type {standard.ExpectedType} to {typeof(T).Name}");
            return new AssetRef<T> { ID = standard.ID, ExpectedType = standard.ExpectedType };
        }
    }
}
