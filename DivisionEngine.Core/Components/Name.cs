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
namespace DivisionEngine.Components
{
    /// <summary>
    /// Special tag component allowing the naming of entities.
    /// </summary>
    public class Name : IComponent
    {
        /// <summary>
        /// Creates a new null name component.
        /// </summary>
        public Name()
        {
            name = null;
        }

        /// <summary>
        /// Builds a name component with name.
        /// </summary>
        /// <param name="name">Name to set component to</param>
        public Name(string name)
        {
            this.name = name;
        }

        public string? name;

        public IComponent Clone() => new Name
        {
            name = name,
        };
    }
}
