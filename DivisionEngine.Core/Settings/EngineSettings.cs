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

namespace DivisionEngine.Settings
{
    /// <summary>
    /// Stores all common settings for the Division Engine.
    /// </summary>
    public class EngineSettings : BaseSettings
    {
        private static EngineSettings? instance;

        /// <summary>
        /// Gets the singleton instance of the engine settings used to configure the application.
        /// </summary>
        /// <remarks>Accessing this property ensures that only one instance of the engine settings is
        /// created and used throughout the application's lifetime. The settings are retrieved via the settings manager,
        /// which may load them from a configuration file or other persistent storage. This property is thread-safe and
        /// intended for global access to engine configuration.</remarks>
        [JsonIgnore]
        public static EngineSettings Instance
        {
            get
            {
                instance ??= SettingsManager.GetSettings<EngineSettings>();
                return instance;
            }
        }

        /// <summary>
        /// Engine settings file identifier.
        /// </summary>
        public override string ID => "EngineSettings";

        // Load will happen through SettingsManager
        public EngineSettings() { }

        protected override void InitializeDefaults()
        {
            ResolutionWidth = 1920;
            ResolutionHeight = 1080;
            Fullscreen = false;
            VSync = true;
            MaxFPS = 0;
            MouseSensitivity = 0.5f;
        }

        [JsonIgnore]
        public int ResolutionWidth
        {
            get => Get(nameof(ResolutionWidth), 1920);
            set => Set(nameof(ResolutionWidth), value);
        }

        [JsonIgnore]
        public int ResolutionHeight
        {
            get => Get(nameof(ResolutionHeight), 1080);
            set => Set(nameof(ResolutionHeight), value);
        }

        [JsonIgnore]
        public bool Fullscreen
        {
            get => Get(nameof(Fullscreen), false);
            set => Set(nameof(Fullscreen), value);
        }

        [JsonIgnore]
        public bool VSync
        {
            get => Get(nameof(VSync), true);
            set => Set(nameof(VSync), value);
        }

        [JsonIgnore]
        public int MaxFPS
        {
            get => Get(nameof(MaxFPS), 0);
            set => Set(nameof(MaxFPS), value);
        }

        [JsonIgnore]
        public float MouseSensitivity
        {
            get => Get(nameof(MouseSensitivity), 1f);
            set => Set(nameof(MouseSensitivity), Math.Clamp(value, 0.01f, 20f));
        }

        // Ideas for later:

        //[JsonIgnore]
        //public int MaxRaySteps
        //{
        //    get => Get(nameof(MaxRaySteps), 128);
        //    set => Set(nameof(MaxRaySteps), value);
        //}

        //[JsonIgnore]
        //public bool ShadowsEnabled
        //{
        //    get => Get(nameof(ShadowsEnabled), true);
        //    set => Set(nameof(ShadowsEnabled), value);
        //}

        //[JsonIgnore]
        //public int ShadowQuality
        //{
        //    get => Get(nameof(ShadowQuality), 2);
        //    set => Set(nameof(ShadowQuality), Math.Clamp(value, 0, 2));
        //}

        //[JsonIgnore]
        //public float RenderScale
        //{
        //    get => Get(nameof(RenderScale), 1.0f);
        //    set => Set(nameof(RenderScale), Math.Clamp(value, 0.25f, 2.0f));
        //}

        //[JsonIgnore]
        //public float MasterVolume
        //{
        //    get => Get(nameof(MasterVolume), 1.0f);
        //    set => Set(nameof(MasterVolume), Math.Clamp(value, 0f, 1f));
        //}

        //[JsonIgnore]
        //public float MusicVolume
        //{
        //    get => Get(nameof(MusicVolume), 0.8f);
        //    set => Set(nameof(MusicVolume), Math.Clamp(value, 0f, 1f));
        //}

        //[JsonIgnore]
        //public float SFXVolume
        //{
        //    get => Get(nameof(SFXVolume), 1.0f);
        //    set => Set(nameof(SFXVolume), Math.Clamp(value, 0f, 1f));
        //}

        //[JsonIgnore]
        //public bool InvertY
        //{
        //    get => Get(nameof(InvertY), false);
        //    set => Set(nameof(InvertY), value);
        //}

        //[JsonIgnore]
        //public Dictionary<string, string> KeyBindings
        //{
        //    get => Get(nameof(KeyBindings), new Dictionary<string, string>());
        //    set => Set(nameof(KeyBindings), value);
        //}

        //[JsonIgnore]
        //public string Language
        //{
        //    get => Get(nameof(Language), "en-US");
        //    set => Set(nameof(Language), value);
        //}

        //[JsonIgnore]
        //public bool ShowTutorials
        //{
        //    get => Get(nameof(ShowTutorials), true);
        //    set => Set(nameof(ShowTutorials), value);
        //}

        //[JsonIgnore]
        //public int Difficulty
        //{
        //    get => Get(nameof(Difficulty), 1);
        //    set => Set(nameof(Difficulty), Math.Clamp(value, 0, 2));
        //}

        /// <summary>
        /// Ensures that the resolution width and height meet the minimum required values when the component is loaded.
        /// </summary>
        /// <remarks>This method is typically called during the component's initialization process to
        /// enforce minimum resolution constraints. Setting the resolution to at least 640x480 pixels may be necessary
        /// for correct rendering or to meet application requirements.</remarks>
        public override void OnLoad()
        {
            if (ResolutionWidth < 640) ResolutionWidth = 640;
            if (ResolutionHeight < 480) ResolutionHeight = 480;
        }
    }
}
