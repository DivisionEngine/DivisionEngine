namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to cast and receive shadows.
    /// </summary>
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
