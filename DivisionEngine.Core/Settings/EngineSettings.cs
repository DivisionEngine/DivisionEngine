using System.Text.Json.Serialization;

namespace DivisionEngine.Settings
{
    /// <summary>
    /// Stores all common settings for the Division Engine.
    /// </summary>
    public class EngineSettings : BaseSettings
    {
        private static EngineSettings? _instance;

        [JsonIgnore]
        public static EngineSettings Instance
        {
            get
            {
                _instance ??= SettingsManager.GetSettings<EngineSettings>();
                return _instance;
            }
        }

        public override string ID => "EngineSettings";

        public EngineSettings() { }

        protected override void InitializeDefaults()
        {
            ResolutionWidth = 1920;
            ResolutionHeight = 1080;
            Fullscreen = false;
            VSync = true;
            MaxFPS = 0;
            MaxRaySteps = 128;
            ShadowsEnabled = true;
            ShadowQuality = 2;
            RenderScale = 1.0f;
            MasterVolume = 1.0f;
            MusicVolume = 0.8f;
            SFXVolume = 1.0f;
            MouseSensitivity = 0.5f;
            InvertY = false;
            KeyBindings = new Dictionary<string, string>();
            Language = "en-US";
            ShowTutorials = true;
            Difficulty = 1;
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
        public int MaxRaySteps
        {
            get => Get(nameof(MaxRaySteps), 128);
            set => Set(nameof(MaxRaySteps), value);
        }

        [JsonIgnore]
        public bool ShadowsEnabled
        {
            get => Get(nameof(ShadowsEnabled), true);
            set => Set(nameof(ShadowsEnabled), value);
        }

        [JsonIgnore]
        public int ShadowQuality
        {
            get => Get(nameof(ShadowQuality), 2);
            set => Set(nameof(ShadowQuality), Math.Clamp(value, 0, 2));
        }

        [JsonIgnore]
        public float RenderScale
        {
            get => Get(nameof(RenderScale), 1.0f);
            set => Set(nameof(RenderScale), Math.Clamp(value, 0.25f, 2.0f));
        }

        [JsonIgnore]
        public float MasterVolume
        {
            get => Get(nameof(MasterVolume), 1.0f);
            set => Set(nameof(MasterVolume), Math.Clamp(value, 0f, 1f));
        }

        [JsonIgnore]
        public float MusicVolume
        {
            get => Get(nameof(MusicVolume), 0.8f);
            set => Set(nameof(MusicVolume), Math.Clamp(value, 0f, 1f));
        }

        [JsonIgnore]
        public float SFXVolume
        {
            get => Get(nameof(SFXVolume), 1.0f);
            set => Set(nameof(SFXVolume), Math.Clamp(value, 0f, 1f));
        }

        [JsonIgnore]
        public float MouseSensitivity
        {
            get => Get(nameof(MouseSensitivity), 0.5f);
            set => Set(nameof(MouseSensitivity), Math.Clamp(value, 0.1f, 2.0f));
        }

        [JsonIgnore]
        public bool InvertY
        {
            get => Get(nameof(InvertY), false);
            set => Set(nameof(InvertY), value);
        }

        [JsonIgnore]
        public Dictionary<string, string> KeyBindings
        {
            get => Get(nameof(KeyBindings), new Dictionary<string, string>());
            set => Set(nameof(KeyBindings), value);
        }

        [JsonIgnore]
        public string Language
        {
            get => Get(nameof(Language), "en-US");
            set => Set(nameof(Language), value);
        }

        [JsonIgnore]
        public bool ShowTutorials
        {
            get => Get(nameof(ShowTutorials), true);
            set => Set(nameof(ShowTutorials), value);
        }

        [JsonIgnore]
        public int Difficulty
        {
            get => Get(nameof(Difficulty), 1);
            set => Set(nameof(Difficulty), Math.Clamp(value, 0, 2));
        }

        public override void OnLoad()
        {
            // Ensure resolution is valid
            if (ResolutionWidth < 640) ResolutionWidth = 640;
            if (ResolutionHeight < 480) ResolutionHeight = 480;
        }
    }
}
