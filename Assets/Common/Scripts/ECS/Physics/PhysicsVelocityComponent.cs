using Unity.Entities;
using Unity.Mathematics;

namespace LittlePhysics
{
    public struct PhysicsVelocityComponent : IComponentData
    {
        public float3 Linear;
        public float3 Angular;

        public VelocityData ToVelocityData()
        {
            return new VelocityData
            {
                Linear = Linear,
                Angular = Angular
            };
        }
    }
}
