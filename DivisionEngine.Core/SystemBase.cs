namespace DivisionEngine
{
    public abstract class SystemBase
    {
        public virtual void Awake() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Render() { }
    }
}
