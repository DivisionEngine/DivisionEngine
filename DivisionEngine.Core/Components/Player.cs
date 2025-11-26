namespace DivisionEngine.Components
{
    public class Player : IComponent
    {
        public static Player Default => new Player
        {
            movementSpeed = 2f,
            mouseSensitivity = 0.001f,
            sprintMultiplier = 2f
        };

        public float movementSpeed;
        public float mouseSensitivity;
        public float sprintMultiplier;
    }
}
