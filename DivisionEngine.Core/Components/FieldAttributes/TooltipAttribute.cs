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
    /// Used for specifying tooltips on fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class TooltipAttribute : Attribute
    {
        /// <summary>
        /// Tooltip text.
        /// </summary>
        public string Tooltip { get; }

        /// <summary>
        /// Applies a tooltip to field.
        /// </summary>
        /// <param name="tooltip">Tooltip text</param>
        public TooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}
