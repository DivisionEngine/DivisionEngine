using DivisionEngine.Components;
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
