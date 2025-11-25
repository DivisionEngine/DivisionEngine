namespace DivisionEngine.Systems
{
    public class CameraSystem() : SystemBase
    {
        public override void Awake()
        {
            Debug.Info("Hello from camera awake!");
        }

        public override void Update()
        {
            Debug.Info("Hello from camera update!");
        }

        public override void Render()
        {
            Debug.Info("Hello from camera render!");
        }
    }
}
