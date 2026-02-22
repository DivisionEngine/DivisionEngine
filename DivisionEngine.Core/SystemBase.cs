//
// Copyright (C) 2026 Rex Woodfield
//
// This file is part of Division Engine.
//
// Division Engine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Division Engine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Division Engine.  If not, see <https://www.gnu.org/licenses/>.
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
