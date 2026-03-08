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
using DivisionEngine.Systems;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DivisionEngine.Settings
{
    /// <summary>
    /// Base class for settings with helper methods.
    /// </summary>
    public abstract class BaseSettings : ISettings
    {
        /// <summary>
        /// Identifier of the settings file.
        /// </summary>
        public abstract string ID { get; }

        [JsonInclude]
        public Dictionary<string, object> Settings { get; private set; } = [];

        protected BaseSettings()
        {
            InitializeDefaults();
        }

        /// <summary>
        /// Initializes default values.
        /// </summary>
        protected virtual void InitializeDefaults() { }

        /// <summary>
        /// Merges default settings with any new settings created.
        /// </summary>
        public void MergeDefaults()
        {
            // Create a temporary instance with defaults
            BaseSettings defaults = (BaseSettings)Activator.CreateInstance(GetType())!;

            // Add any missing keys from defaults
            foreach (var kvp in defaults.Settings)
                if (!Settings.ContainsKey(kvp.Key))
                    Settings[kvp.Key] = kvp.Value;
        }

        /// <summary>
        /// Get a setting value with type safety.
        /// </summary>
        protected T Get<T>(string key, T defaultValue)
        {
            if (Settings.TryGetValue(key, out object? value))
            {
                try
                {
                    // Handle JsonElement from deserialization
                    if (value is JsonElement jsonElement) return jsonElement.Deserialize<T>() ?? defaultValue;
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    Debug.Info($"Settings System: Could not load setting {key} of type {typeof(T)}");
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Set a setting value.
        /// </summary>
        protected void Set<T>(string key, T value)
        {
            if (value == null) Debug.Warning($"Settings System: Value of {key} setting is null");
            SettingsSystem.MarkDirty(); // Don't forget to mark settings dirty
            Settings[key] = value!;
        }

        public virtual void OnLoad() { }
        public virtual void OnSave() { }
    }
}
