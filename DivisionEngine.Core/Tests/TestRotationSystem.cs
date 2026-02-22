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
namespace DivisionEngine.Tests
{
    internal class TestRotationSystem : SystemBase
    {
        public override void Update()
        {
            //TestSystemNumericsRotation();
        }

        public static void TestSystemNumericsRotation()
        {
            Debug.Info("=== TESTING SYSTEM.NUMERICS ROTATION ===");

            // Test with System.Numerics directly
            System.Numerics.Vector3 testVector = new System.Numerics.Vector3(0, 0, -1);
            System.Numerics.Quaternion testRot = System.Numerics.Quaternion.CreateFromAxisAngle(
                System.Numerics.Vector3.UnitY, MathF.PI / 2);

            System.Numerics.Vector3 result = System.Numerics.Vector3.Transform(testVector, testRot);
            Debug.Error($"System.Numerics: (0,0,-1) rotated 90° around Y = {result}");
            // Should be (-1, 0, 0)
        }
    }
}
