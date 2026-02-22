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
namespace DivisionEngine.Components.FieldAttributes
{
    /// <summary>
    /// Used for specifying float4 fields as quaternions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class RotationAttribute : Attribute
    {
        /// <summary>
        /// Whether to display in degrees or radians.
        /// </summary>
        public bool Degrees { get; set; } = true;

        /// <summary>
        /// Creates a new RotationAttribute with default settings.
        /// </summary>
        public RotationAttribute() { }

        /// <summary>
        /// Creates a new RotationAttribute with degrees parameter.
        /// </summary>
        /// <param name="degrees">Whether to display in degrees or radians</param>
        public RotationAttribute(bool degrees)
        {
            Degrees = degrees;
        }
    }
}
