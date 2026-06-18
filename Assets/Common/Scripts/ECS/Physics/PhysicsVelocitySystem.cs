using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace LittlePhysics
{
    [BurstCompile]
    [UpdateInGroup(typeof(LittlePhysicsLateSystemGroup), OrderLast = true)]
    public partial struct PhysicsVelocitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSingleton>();
            state.RequireForUpdate<LittlePhysicsTimeComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<PhysicsSingleton>();
            var time = SystemAPI.GetSingleton<LittlePhysicsTimeComponent>();

            var combinedDep = JobHandle.CombineDependencies(state.Dependency, singleton.PhysicsJobHandle);
            var settings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();

            int bodyCount = settings.BlobRef.Value.LodData.MaxEntityCount;

            state.Dependency = new ApplyVelocitiesJob
            {
                BodiesList = singleton.BodiesList,
                DeltaTime = time.DeltaTime,
            }.Schedule(bodyCount, 32, combinedDep);

            singleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(singleton);
        }

        [BurstCompile]
        private struct ApplyVelocitiesJob : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<PhysicsBodyData> BodiesList;
            public float DeltaTime;

            public void Execute(int index)
            {
                var body = BodiesList[index];

                if (body.Main == Entity.Null)
                {
                    return;
                }

                if (!body.IsDynamic)
                {
                    return;
                }

                body.PositionData.Position += body.VelocityData.Linear * DeltaTime;
                body.PositionData.RotationOffset += body.VelocityData.Angular * DeltaTime;

                BodiesList[index] = body;
            }
        }
    }
}
