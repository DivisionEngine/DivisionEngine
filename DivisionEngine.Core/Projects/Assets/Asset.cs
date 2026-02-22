using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivisionEngine.Projects.Assets
{
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

    public class AssetRef(string guid, AssetType type)
    {
        public string Guid { get; set; } = guid;
        public string RelativePath { get; set; } = string.Empty;
        public AssetType ExpectedType { get; set; } = type;

        public bool IsValid() => !string.IsNullOrEmpty(Guid);
    }

    public abstract class Asset
    {
        public string Guid { get; protected set; }
        public string RelativePath { get; protected set; }
        public AssetMetadata Metadata { get; protected set; }
        public bool IsLoaded { get; protected set; }

        protected Asset(AssetMetadata metadata)
        {
            Metadata = metadata;
            Guid = metadata.ID;
            RelativePath = metadata.RelativePath;
        }

        public abstract Task<bool> LoadAsync();
        public abstract void Unload();
    }
}
