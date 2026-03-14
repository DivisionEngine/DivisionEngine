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
    /// Represents the metadata for asset files.
    /// </summary>
    public class AssetMetadata
    {
        /// <summary>
        /// Asset GUID.
        /// </summary>
        public string ID { get; set; } = Guid.CreateVersion7().ToString();
        
        /// <summary>
        /// Asset file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Relative path of asset file to assets folder.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Asset type.
        /// </summary>
        public AssetType Type { get; set; } = AssetType.None;
        
        /// <summary>
        /// Last time asset file was modified.
        /// </summary>
        public DateTime LastModified { get; set; }
        
        /// <summary>
        /// Size of asset file.
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Tags for asset.
        /// </summary>
        public List<string> Tags { get; set; } = [];
        
        /// <summary>
        /// Custom properties of asset.
        /// </summary>
        public Dictionary<string, object> CustomProperties { get; set; } = [];
    }
}
