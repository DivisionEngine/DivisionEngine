namespace DivisionEngine.Components.SDFs.Effects
{
    public class SoftShadows : IComponent
    {
        public SoftShadows()
        {
            shadowCaster = true;
            shadowReceiver = true;
        }

        public bool shadowCaster;
        public bool shadowReceiver;
    }
}
