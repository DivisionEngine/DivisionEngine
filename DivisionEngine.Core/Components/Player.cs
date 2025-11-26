namespace DivisionEngine.Components
{
    public class Player : IComponent
    {
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
