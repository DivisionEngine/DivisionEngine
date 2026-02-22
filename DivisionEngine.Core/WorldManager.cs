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
using DivisionEngine.MathLib;
using Environment = DivisionEngine.Components.Environment;
using Random = DivisionEngine.MathLib.Random;

namespace DivisionEngine
{
    /// <summary>
    /// Manages all worlds loaded and currently active.
    /// </summary>
    public static class WorldManager
    {
        /// <summary>
        /// Current active world.
        /// </summary>
        public static World? CurrentWorld { get; private set; }
        private static readonly Dictionary<string, World> worlds = [];

        /// <summary>
        /// Creates a new default world.
        /// </summary>
        /// <param name="makeCurrent">Makes the newly created default world the current world</param>
        /// <returns>The new default world</returns>
        public static World CreateDefaultWorld(bool makeCurrent)
        {
            World newDefaultWorld = new World("default");
            newDefaultWorld.RegisterAllSystems();

            // Environment setup
            uint cameraEntity = newDefaultWorld.CreateEntity("Camera");
            newDefaultWorld.AddComponent(cameraEntity, new Transform
            {
                position = new float3(0, 2, 7),
            });
            newDefaultWorld.AddComponent(cameraEntity, new Camera());
            newDefaultWorld.AddComponent(cameraEntity, new Player());

            uint environmentEntity = newDefaultWorld.CreateEntity("Environment");
            newDefaultWorld.AddComponent(environmentEntity, new Environment());

            // Spheres
            int sphereCount = 1;
            for (int i = 0; i < sphereCount; i++)
            {
                for (int j = 0; j < sphereCount; j++)
                {
                    uint sphereEntity = newDefaultWorld.CreateEntity("Sphere");
                    newDefaultWorld.AddComponent(sphereEntity, new Transform
                    {
                        position = new float3((i - sphereCount / 2) * 5, 1, (j - sphereCount / 2) * 5),
                    });
                    newDefaultWorld.AddComponent(sphereEntity, new SDFSphere
                    {
                        radius = 2f,
                    });
                    newDefaultWorld.AddComponent(sphereEntity, new SDFMaterial
                    {
                        albedoColor = ColorPalette.Khaki,
                        roughness = 0.1f, //1f - i / (float)(sphereCount - 1),
                        metallic = 1f, //1f - j / (float)(sphereCount - 1),
                        ior = 1.5f,
                    });
                    newDefaultWorld.AddComponent(sphereEntity, new SoftShadows());
                    newDefaultWorld.AddComponent(sphereEntity, new Reflections());
                    newDefaultWorld.AddComponent(sphereEntity, new Refractions());
                }
            }

            uint roundedBoxEntity = newDefaultWorld.CreateEntity("Rounded Box");
            newDefaultWorld.AddComponent(roundedBoxEntity, new Transform
            {
                position = new float3(0, -4, 0),
            });
            newDefaultWorld.AddComponent(roundedBoxEntity, new SDFRoundedBox
            {
                size = new float3(40f, 1f, 40f),
                bevel = 0.25f,
            });
            newDefaultWorld.AddComponent(roundedBoxEntity, new SDFMaterial
            {
                albedoColor = ColorPalette.DeepSkyBlue,
            });
            newDefaultWorld.AddComponent(roundedBoxEntity, new SoftShadows());
            newDefaultWorld.AddComponent(roundedBoxEntity, new Reflections());

            uint boxEntity = newDefaultWorld.CreateEntity("Box");
            newDefaultWorld.AddComponent(boxEntity, new Transform
            {
                position = new float3(5, 3, -5),
                rotation = Quaternion.CreateFromYawPitchRoll(Random.NextFloat(), Random.NextFloat(), Random.NextFloat()),
            });
            newDefaultWorld.AddComponent(boxEntity, new SDFBox
            {
                size = new float3(1f, 2f, 1f),
            });
            newDefaultWorld.AddComponent(boxEntity, new SDFMaterial
            {
                albedoColor = ColorPalette.Crimson,
            });
            newDefaultWorld.AddComponent(boxEntity, new SoftShadows());
            newDefaultWorld.AddComponent(boxEntity, new Reflections());

            SetWorld(newDefaultWorld);
            if (makeCurrent)
            {
                EngineCore.Stop();
                CurrentWorld = newDefaultWorld;
                EngineCore.Start();
            }
            return newDefaultWorld;
        }

        /// <summary>
        /// Sets / adds a world based off its name.
        /// </summary>
        /// <param name="world">World to add / set</param>
        public static void SetWorld(World world)
        {
            if (!worlds.TryAdd(world.Name, world))
                worlds[world.Name] = world;
        }

        /// <summary>
        /// Checks if a world exists in the world manager.
        /// </summary>
        /// <param name="name">Name to check for</param>
        /// <returns>If world exists</returns>
        public static bool HasWorld(string name) => worlds.ContainsKey(name);

        /// <summary>
        /// Gets a world based off a certain name.
        /// </summary>
        /// <param name="name">Name of the world to retrieve</param>
        /// <returns>The world referenced by name</returns>
        public static World? GetWorld(string name)
        {
            worlds.TryGetValue(name, out var world);
            return world;
        }

        /// <summary>
        /// Switches the current world to the one referenced by a certain name.
        /// </summary>
        /// <param name="name">Name of the world to make current</param>
        /// <returns>Whether or not the switch was successful</returns>
        public static bool SwitchWorld(string name)
        {
            if (worlds.TryGetValue(name, out var world))
            {
                EngineCore.Stop();
                CurrentWorld = world;
                EngineCore.Start();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a world from the world manager.
        /// </summary>
        /// <param name="name">Name of the world to remove</param>
        /// <returns>Whether the world could be removed or not</returns>
        public static bool RemoveWorld(string name)
        {
            if (CurrentWorld == worlds[name]) return false;
            return worlds.Remove(name);
        }
    }
}
