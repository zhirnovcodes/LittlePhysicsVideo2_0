using LittlePhysics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
[UpdateInGroup(typeof(LittlePhysicsUserSystemGroup))]
public partial struct AtomsRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsSingleton>();
        state.RequireForUpdate<AtomsRotationComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var singleton = SystemAPI.GetSingleton<PhysicsSingleton>();
        var settings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();
        int bodyCount = settings.BlobRef.Value.LodData.MaxEntityCount;

        var combinedDep = JobHandle.CombineDependencies(state.Dependency, singleton.PhysicsJobHandle);

        state.Dependency = new ApplyOrbitSpinJob
        {
            BodiesList = singleton.BodiesList,
            CollisionDataMap = singleton.Collisions.CollisionDataMap,
            AtomsRotation = SystemAPI.GetSingleton<AtomsRotationComponent>(),
            BodiesCount = bodyCount
        }.Schedule(bodyCount, 32, combinedDep);

        singleton.PhysicsJobHandle = state.Dependency;
        SystemAPI.SetSingleton(singleton);
    }

    [BurstCompile]
    private struct ApplyOrbitSpinJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction] public NativeArray<PhysicsBodyData> BodiesList;
        [NativeDisableContainerSafetyRestriction] public ListsArray<CollisionData> CollisionDataMap;
        [ReadOnly] public AtomsRotationComponent AtomsRotation;
        public int BodiesCount;

        public void Execute(int index)
        {
            var body = BodiesList[index];

            if (!body.IsDynamic)
            {
                return;
            }

            if (body.IsTrigger)
            {
                return;
            }

            int collisionCount = CollisionDataMap.GetCount(index);
            if (collisionCount == 0)
            {
                return;
            }

            var vel = body.VelocityData;

            float3 up0 = new float3(-1f, -1f, -1f);
            float3 up1 = new float3(1f, 1f, 1f);
            float upT = index / ((float)BodiesCount/3f);
            float3 up = math.normalize( math.lerp(up0, up1, upT));
            if (math.lengthsq(up) <= 0)
            {
                up = new float3(0,1,0);
            }

            for (int i = 0; i < collisionCount; i++)
            {
                var col = CollisionDataMap.GetValue(index, i);

                if (col.Other == Entity.Null)
                {
                    continue;
                }

                int otherIndex = (int)col.OtherIndex;

                var otherBody = BodiesList[otherIndex];

                if (!otherBody.IsTrigger)
                {
                    continue;
                }

                float3 currentPosition = body.PositionData.Position;
                float3 otherPosition = otherBody.PositionData.Position;

                float3 direction = math.normalize(otherPosition - currentPosition);

                float3 right = math.cross(direction, up);

                float3 attraction = AtomsRotation.AttractionPower * direction;
                float3 rotation = right * AtomsRotation.Speed;

                vel.Linear += attraction + rotation;
            }

            body.VelocityData = vel;
            BodiesList[index] = body;
        }
    }
}
