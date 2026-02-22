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
using DivisionEngine.Components;
using DivisionEngine.Components.SDFs;
using DivisionEngine.Components.SDFs.Effects;
using DivisionEngine.Components.SDFs.Primitives;

namespace DivisionEngine.Editor
{
    /// <summary>
    /// Utility used for creating default entities from the editor.
    /// </summary>
    public static class DefaultEntities
    {
        public static uint Empty(string name = "New Entity")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            return W.CreateEntity(name);
        }

        public static uint EmptyTransform(string name = "New Entity")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            return W.CreateTransformEntity(name);
        }

        public static uint Camera(string name = "New Camera", bool hasPlayerControls = true)
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint camera = W.CreateTransformEntity(name);
            W.AddComponent(camera, new Camera());
            if (hasPlayerControls) W.AddComponent(camera, new Player());
            return camera;
        }

        public static uint Environment(string name = "New Environment")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint environment = W.CreateEntity(name);
            W.AddComponent(environment, new Environment());
            return environment;
        }

        public static uint SDFSphere(string name = "New Sphere")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFSphere());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFBox(string name = "New Box")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFBox());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFRoundedBox(string name = "New Rounded Box")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFRoundedBox());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFTorus(string name = "New Donut")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFTorus());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFPyramid(string name = "New Pyramid")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFPyramid());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFPlane(string name = "New Plane")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFPlane());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFCylinder(string name = "New Cylinder")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFCylinder());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFCapsule(string name = "New Capsule")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFCapsule());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }

        public static uint SDFCone(string name = "New Cone")
        {
            if (WorldManager.CurrentWorld == null)
                Debug.Warning("No world is currently loaded to add entities to");
            uint sdf = W.CreateTransformEntity(name);
            W.AddComponent(sdf, new SDFCone());
            W.AddComponent(sdf, new SDFMaterial());
            W.AddComponent(sdf, new SoftShadows());
            W.AddComponent(sdf, new Reflections());
            W.AddComponent(sdf, new Refractions());
            return sdf;
        }
    }
}
