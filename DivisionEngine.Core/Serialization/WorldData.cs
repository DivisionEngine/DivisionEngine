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
using System.Text.Json.Serialization;

namespace DivisionEngine.Serialization
{
    /// <summary>
    /// Class used for serializing the world data to a project file.
    /// </summary>
    public class WorldData
    {
        public string Name { get; set; }
        public uint NextEntityId { get; set; }
        public List<EntityData> Entities { get; set; }

        /// <summary>
        /// Shortcut for "new WorldData(WorldManager.CurrentWorld!)".
        /// </summary>
        [JsonIgnore] public static WorldData Current => new WorldData(WorldManager.CurrentWorld!);

        [JsonConstructor]
        public WorldData()
        {
            Name = string.Empty;
            Entities = [];
        }

        /// <summary>
        /// Builds serializable world data object automatically from ECS world.
        /// </summary>
        /// <param name="world">World object to serialize</param>
        public WorldData(World world)
        {
            Name = world.Name;
            NextEntityId = world.NextEntityId;
            Entities = [];
            foreach (uint entity in world.entities)
                Entities.Add(new EntityData(entity, world));
        }
    }
}
