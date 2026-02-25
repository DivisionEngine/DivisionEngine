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
    /// Represents the project asset database file, containing references to all current metadata.
    /// </summary>
    public class ProjectAssetDatabase
    {
        /// <summary>
        /// Project path.
        /// </summary>
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// All folder metadata, organized by relative folder path.
        /// </summary>
        public Dictionary<string, FolderMetadata> Folders { get; set; } = []; // Key = relative folder path

        /// <summary>
        /// All asset metadata, organized by asset GUID. 
        /// </summary>
        public Dictionary<string, AssetMetadata> AllAssetsByID { get; set; } = []; // Master GUID
    }
}
