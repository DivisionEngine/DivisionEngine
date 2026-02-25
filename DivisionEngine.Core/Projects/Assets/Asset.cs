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
    /// Type of an asset.
    /// </summary>
    public enum AssetType
    {
        None,
        Texture,
        Script,
        SDF,
        Material,
        Audio,
        Font,
    }

    /// <summary>
    /// Reference to a loaded asset.
    /// </summary>
    /// <param name="id">GUID of asset</param>
    /// <param name="type">Type of asset</param>
    public class AssetRef(string id, AssetType type)
    {
        public string ID { get; set; } = id;
        public string RelativePath { get; set; } = string.Empty;
        public AssetType ExpectedType { get; set; } = type;

        public bool IsValid() => !string.IsNullOrEmpty(ID);
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
}
