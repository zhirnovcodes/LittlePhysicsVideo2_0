using Unity.Entities;
using Unity.Mathematics;

namespace LittlePhysics
{
    public struct UnityPhysicsPushForceComponent : IComponentData
    {
        public float3 Force;
        public bool IsPushed;
    }
}
