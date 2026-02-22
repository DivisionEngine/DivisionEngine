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
    /// Stores entity data for serializing project entities.
    /// </summary>
    public class EntityData
    {
        public uint Id { get; set; }
        public List<ComponentData> Components { get; set; }

        [JsonConstructor]
        public EntityData()
        {
            Components = [];
        }

        public EntityData(uint entity, World world)
        {
            Id = entity;
            Components = [];
            List<IComponent> comps = world.GetAllComponents(entity);
            for (int i = 0; i < comps.Count; i++)
                Components.Add(new ComponentData(comps[i]));
        }
    }
}
