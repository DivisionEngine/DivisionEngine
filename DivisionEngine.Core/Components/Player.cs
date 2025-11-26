namespace DivisionEngine.Components
{
    public class Player : IComponent
    {
        public Player()
        {
            movementSpeed = 2f;
            mouseSensitivity = 20f;
            sprintMultiplier = 2f;
        }

        public float movementSpeed;
        public float mouseSensitivity;
        public float sprintMultiplier;
    }
}
