namespace DivisionEngine.Tests
{
    internal class TestRotationSystem : SystemBase
    {
        public override void Update()
        {
            TestSystemNumericsRotation();
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
