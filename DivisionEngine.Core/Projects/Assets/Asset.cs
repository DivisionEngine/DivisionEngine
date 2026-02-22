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
