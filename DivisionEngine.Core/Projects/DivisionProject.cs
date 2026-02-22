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
using System.Text.Json.Serialization;

namespace DivisionEngine.Projects
{
    /// <summary>
    /// Represents a project in the Division Engine, used for serializing project data.
    /// </summary>
    public class DivisionProject
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime LastSaved { get; set; }

        [JsonConstructor]
        public DivisionProject()
        {
            Name = string.Empty;
            Version = string.Empty;
        }

        public DivisionProject(string name = "New Project")
        {
            LastSaved = DateTime.Now;
            Name = name;
            Version = "1.0.0";
        }
    }
}
