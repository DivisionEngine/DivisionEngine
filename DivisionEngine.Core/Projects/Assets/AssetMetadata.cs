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
        public string ID { get; set; } = Guid.CreateVersion7().ToString();
        public string FileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public AssetType Type { get; set; } = AssetType.None;
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        public List<string> Tags { get; set; } = [];
        public Dictionary<string, object> CustomProperties { get; set; } = [];
    }
}
