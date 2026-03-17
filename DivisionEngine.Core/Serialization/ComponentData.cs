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

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Represents serializable data for a component in Division Engine.
    /// </summary>
    public class ComponentData
    {
        /// <summary>
        /// Type name of component.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Name of encapsulating assembly for component type.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Serialized list of component properties.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        [JsonConstructor]
        public ComponentData()
        {
            TypeName = string.Empty;
            AssemblyName = string.Empty;
            Properties = [];
        }

        /// <summary>
        /// Generates serialized component data.
        /// </summary>
        /// <param name="component">Component to serialize</param>
        public ComponentData(IComponent component)
        {
            TypeName = component.GetType().Name;
            AssemblyName = component.GetType().Assembly.FullName!;
            Properties = Serialize.Component(component);
        }
    }
}
