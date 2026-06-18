using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace LittlePhysics
{
    public sealed class UnityPhysicsPushForceAuthoring : MonoBehaviour
    {
        public Vector3 Force;
        public bool IsPushed;

        private sealed class Baker : Baker<UnityPhysicsPushForceAuthoring>
        {
            public override void Bake(UnityPhysicsPushForceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnityPhysicsPushForceComponent
                {
                    Force = authoring.Force,
                    IsPushed = authoring.IsPushed
                });
            }
        }
    }
}
