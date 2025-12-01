namespace DivisionEngine.Components
{
    /// <summary>
    /// Represents an entity with player controls (WASD, LShift, Mouse Look).
    /// </summary>
    public class Player : IComponent
    {
        /// <summary>
        /// Default player controls (speed = 4, mouse sensitivity = 2, sprint multiplier = 2).
        /// </summary>
        public Player()
        {
            movementSpeed = 4f;
            mouseSensitivity = 2f;
            sprintMultiplier = 2f;
        }

        public float movementSpeed;
        public float mouseSensitivity;
        public float sprintMultiplier;
    }
}
