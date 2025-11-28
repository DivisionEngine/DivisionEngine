namespace DivisionEngine
{
    /// <summary>
    /// The base class all systems inherit from.
    /// </summary>
    public abstract class SystemBase
    {
        /// <summary>
        /// Called once when the World is run.
        /// </summary>
        public virtual void Awake() { }

        /// <summary>
        /// Called once every frame.
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Called once every frame after Update loop has completed.
        /// </summary>
        public virtual void FixedUpdate() { }
        
        /// <summary>
        /// Called before every render thread execution step.
        /// </summary>
        public virtual void Render() { }
    }
}
