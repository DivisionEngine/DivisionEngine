namespace DivisionEngine.Components.SDFs.Effects
{
    /// <summary>
    /// Allows SDF objects to cast and receive shadows.
    /// </summary>
    public class SoftShadows : IComponent
    {
        /// <summary>
        /// Shadow casters and recievers both enabled with a max distance of 100.0.
        /// </summary>
        public SoftShadows()
        {
            shadowCaster = true;
            shadowReceiver = true;

            minDistance = 0.001f;
            maxDistance = 100f;
        }

        public bool shadowCaster;
        public bool shadowReceiver;

        public float minDistance;
        public float maxDistance;
    }
}
