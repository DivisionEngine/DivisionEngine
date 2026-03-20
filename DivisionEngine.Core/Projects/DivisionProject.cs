//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
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
