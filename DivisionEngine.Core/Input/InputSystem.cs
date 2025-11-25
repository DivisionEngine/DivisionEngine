namespace DivisionEngine.Input
{
    /// <summary>
    /// Represents a key code in the input system.
    /// </summary>
    public enum KeyCode
    {
        // Letters
        A = 0, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

        // Numbers (Top row)
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,

        // Function keys
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

        // Control keys
        Escape,
        Tab,
        CapsLock,
        ShiftLeft,
        ShiftRight,
        ControlLeft,
        ControlRight,
        AltLeft,
        AltRight,
        Space,
        Enter,
        Backspace,

        // Navigation keys
        Insert,
        Delete,
        Home,
        End,
        PageUp,
        PageDown,

        // Arrow keys
        ArrowUp,
        ArrowDown,
        ArrowLeft,
        ArrowRight,

        // Symbols
        Minus,          // -
        Equals,         // =
        LeftBracket,    // [
        RightBracket,   // ]
        Backslash,      // \
        Semicolon,      // ;
        Quote,          // '
        Comma,          // ,
        Period,         // .
        Slash,          // /
        Grave,          // `

        // Numpad
        NumLock,
        Numpad0,
        Numpad1,
        Numpad2,
        Numpad3,
        Numpad4,
        Numpad5,
        Numpad6,
        Numpad7,
        Numpad8,
        Numpad9,
        NumpadDivide,
        NumpadMultiply,
        NumpadMinus,
        NumpadPlus,
        NumpadEnter,
        NumpadPeriod,

        // Misc
        PrintScreen,
        ScrollLock,
        PauseBreak,
        Menu,
        WindowsLeft,
        WindowsRight,
        Application,

        // Unknown
        Unknown
    }

    /// <summary>
    /// Represents a mouse key code in the input system.
    /// </summary>
    public enum MouseCode
    {
        // Main
        Left,
        Middle,
        Right,

        // Extra
        Button4,
        Button5,
        Button6,
        Button7,
        Button8,
        Button9,
        Button10,
        Button11,
        Button12,
        
        // Unknown
        Unknown
    };

    /// <summary>
    /// Handles input events and maintains input state.
    /// </summary>
    public class InputSystem
    {
        /// <summary>
        /// Instance of the Input class, used for singleton access.
        /// </summary>
        public static InputSystem? Instance { get; private set; }

        private readonly HashSet<KeyCode> pressedKeys;
        private readonly HashSet<MouseCode> pressedMouseKeys;
        private readonly object syncLock;

        /// <summary>
        /// Initializes a new input system singleton.
        /// </summary>
        /// <remarks>(this should only be called once)</remarks>
        public InputSystem()
        {
            syncLock = new object();
            pressedKeys = [];
            pressedMouseKeys = [];
            Instance = this;
        }

        public void SetKeyDown(KeyCode key)
        {
            lock (syncLock) pressedKeys.Add(key);
        }

        public void SetKeyUp(KeyCode key)
        {
            lock (syncLock) pressedKeys.Remove(key);
        }

        public void SetMouseKeyDown(MouseCode mouseKey)
        {
            lock (syncLock) pressedMouseKeys.Add(mouseKey);
        }

        public void SetMouseKeyUp(MouseCode mouseKey)
        {
            lock (syncLock) pressedMouseKeys.Remove(mouseKey);
        }

        /// <summary>
        /// Checks if a key is currently pressed on the default keyboard.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>Whether the key is pressed or not</returns>
        public static bool IsPressed(KeyCode key)
        {
            lock (Instance!.syncLock) return Instance.pressedKeys.Contains(key);
        }

        public static bool IsMousePressed(MouseCode key)
        {
            lock (Instance!.syncLock) return Instance.pressedMouseKeys.Contains(key);
        }
    }
}
