using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace LittlePhysics
{
    /// <summary>
    /// Reads per-body collision records produced by ImpulseDetectionSystem and applies
    /// impulse-derived velocity changes and push-out corrections to each dynamic body.
    /// Each CollisionData entry already contains the impulse and push-out computed for
    /// this specific body's side of the pair, so no further physics calculations are needed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(LittlePhysicsInternalSystemGroup))]
    [UpdateAfter(typeof(CollisionDetectionSystem))]
    public partial struct CollisionVelocitySystem : ISystem
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

            var settings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();
            int bodyCount = settings.BlobRef.Value.LodData.MaxEntityCount;

            var combinedDep = JobHandle.CombineDependencies(state.Dependency, singleton.PhysicsJobHandle);

            state.Dependency = new ApplyCollisionVelocitiesJob
            {
                BodiesList = singleton.BodiesList,
                CollisionDataMap = singleton.Collisions.CollisionDataMap,
                PushOutPower = singleton.Settings.BlobRef.Value.EnvironmentSettings.PushOutPower,
                DeltaTime = time.DeltaTime,
                PushOutToVelocity = settings.BlobRef.Value.EnvironmentSettings.PushOutToVelocity,
            }.Schedule(bodyCount, 32, combinedDep);

            singleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(singleton);
        }

        [BurstCompile]
        private struct ApplyCollisionVelocitiesJob : IJobParallelFor
        {
            /// <summary>
            /// Job i writes only to BodiesList[i].
            /// NativeDisableContainerSafetyRestriction is safe because no two jobs share a write slot.
            /// </summary>
            [NativeDisableContainerSafetyRestriction] public NativeArray<PhysicsBodyData> BodiesList;
            [NativeDisableContainerSafetyRestriction] public ListsArray<CollisionData> CollisionDataMap;
            public float PushOutPower;
            public float DeltaTime;
            public bool PushOutToVelocity;

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

                int count = CollisionDataMap.GetCount(index);
                if (count == 0)
                {
                    return;
                }

                var vel = body.VelocityData;

                for (int i = 0; i < count; i++)
                {
                    var col = CollisionDataMap.GetValue(index, i);

                    if (col.Other == Entity.Null)
                    {
                        continue;
                    }

                    CollisionForces.ImpulseToVelocity(
                        body, col.Impulse, col.ContactPoint,
                        out float3 linearChange, out float3 angularChange);

                    vel.Linear += linearChange;
                    vel.Angular += !body.VelocityData.IsRotationBlocked ? angularChange : float3.zero;

                    if (!PushOutToVelocity)
                    {
                        body.PositionData.Position += col.PushOut * DeltaTime * PushOutPower;
                    }
                    else
                    {
                        vel.Linear += col.PushOut * PushOutPower;
                    }
                }

                body.VelocityData = vel;
                BodiesList[index] = body;
            }
        }
    }
}
