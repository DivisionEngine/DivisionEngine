//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
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
