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
using DivisionEngine.Components.SDFs.Primitives;
using DivisionEngine.Systems;

namespace DivisionEngine.Tests
{
    internal class TestObjMovementSystem : SystemBase
    {
        public override void Update()
        {
            //UpdateBoxes();
        }

        private void UpdateBoxes()
        {
            foreach (var (_, transform, box) in W.QueryData<Transform, SDFBox>())
            {
                float3 curPos = transform.position;
                transform.position = new float3(curPos.X - TimeSystem.DeltaTimeF * 0.5f, curPos.Y, curPos.Z);
                box.size = new float3(box.size.X + TimeSystem.DeltaTimeF * 0.1f, box.size.Y, box.size.Z);
            }
        }
    }
}
