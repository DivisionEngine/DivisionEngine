//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine
{
    /// <summary>
    /// The base class all systems inherit from.
    /// </summary>
    public abstract class SystemBase
    {
        /// <summary>
        /// Called once when the world is run.
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
        /// Called once when the world is stopped or unloaded.
        /// </summary>
        public virtual void Unload() { }
        
        /// <summary>
        /// Called before every render thread execution step.
        /// </summary>
        public virtual void Render() { }
    }
}
