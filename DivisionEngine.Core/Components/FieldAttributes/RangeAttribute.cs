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
    /// Used for specifying integer and float fields as sliders.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        /// <summary>
        /// Minimum slider value.
        /// </summary>
        public float Min { get; }

        /// <summary>
        /// Maximum slider value.
        /// </summary>
        public float Max { get; }

        /// <summary>
        /// Applies a range slider.
        /// </summary>
        /// <param name="min">Minimum slider value</param>
        /// <param name="max">Maximum slider value</param>
        public RangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
