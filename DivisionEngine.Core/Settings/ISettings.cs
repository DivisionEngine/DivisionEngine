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
namespace DivisionEngine.Settings
{
    /// <summary>
    /// Represents a generic serializable settings object.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Identifier of the settings object as a whole.
        /// </summary>
        public string ID { get; }

        /// <summary>
        /// Dictionary of settings values.
        /// </summary>
        public Dictionary<string, object> Settings { get; }

        /// <summary>
        /// Called after loading to validate or initialize settings.
        /// </summary>
        public virtual void OnLoad() { }

        /// <summary>
        /// Called before saving.
        /// </summary>
        public virtual void OnSave() { }
    }
}
