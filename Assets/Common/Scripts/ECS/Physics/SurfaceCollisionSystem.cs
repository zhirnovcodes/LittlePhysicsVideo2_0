using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace LittlePhysics
{
    [BurstCompile]
    [UpdateInGroup(typeof(LittlePhysicsInternalSystemGroup))]
    [UpdateAfter(typeof(CollisionMapUpdateSystem))]
    public partial struct SurfaceCollisionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSingleton>();
            state.RequireForUpdate<CollisionSurfaceComponent>();
            state.RequireForUpdate<LittlePhysicsTimeComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var physicsSingleton = SystemAPI.GetSingleton<PhysicsSingleton>();
            var time = SystemAPI.GetSingleton<LittlePhysicsTimeComponent>();
            var surfaceCollisionMap = physicsSingleton.CollisionMap.SurfaceCollisionMap;

            var physicsHandle = JobHandle.CombineDependencies(state.Dependency, physicsSingleton.PhysicsJobHandle);

            var clearJob = new ClearSurfaceCollisionMapJob
            {
                SurfaceCollisionMap = surfaceCollisionMap,
            }.Schedule(surfaceCollisionMap.Length, 64, physicsHandle);

            var surfaceJob = new CheckDynamicVsSurfaceJob
            {
                SurfaceBody = SystemAPI.GetSingleton<CollisionSurfaceComponent>().ToBodyData(),
                BodiesList = physicsSingleton.BodiesList,
                PhysicsSettings = physicsSingleton.Settings,
                PushOutPower = physicsSingleton.Settings.BlobRef.Value.EnvironmentSettings.PushOutPower,
                DeltaTime = time.DeltaTime,
                SurfaceCollisionMap = surfaceCollisionMap,
            }.Schedule(physicsSingleton.BodiesList.Length, 32, clearJob);

            state.Dependency = surfaceJob;
            physicsSingleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(physicsSingleton);
        }

        [BurstCompile]
        private struct ClearSurfaceCollisionMapJob : IJobParallelFor
        {
            public NativeArray<SurfaceCollisionData> SurfaceCollisionMap;

            public void Execute(int index)
            {
                SurfaceCollisionMap[index] = default;
            }
        }

        [BurstCompile]
        private struct CheckDynamicVsSurfaceJob : IJobParallelFor
        {
            public PhysicsBodyData SurfaceBody;
            [NativeDisableContainerSafetyRestriction] public NativeArray<PhysicsBodyData> BodiesList;
            [NativeDisableContainerSafetyRestriction] public NativeArray<SurfaceCollisionData> SurfaceCollisionMap;
            public PhysicsSettingsComponent PhysicsSettings;
            public float PushOutPower;
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

                if (!PhysicsSettings.IsColliding(body.Layer, SurfaceBody.Layer))
                {
                    return;
                }

                if (!CollisionMethods.AreBodiesColliding(body, SurfaceBody, out float3 contactPoint))
                {
                    return;
                }

                SurfaceCollisionMap[index] = new SurfaceCollisionData { IsColliding = true, ContactPoint = contactPoint };

                var vel = body.VelocityData;

                CollisionForces.GetCollisionImpulse(body, SurfaceBody, vel, default, contactPoint,
                    out float3 impulse);
                CollisionForces.GetPushOutForce(body, SurfaceBody, contactPoint, out float3 pushForce);
                CollisionForces.ImpulseToVelocity(body, impulse, contactPoint,
                    out float3 linearChange, out float3 angularChange);

                body.PositionData.Position += pushForce * DeltaTime * PushOutPower;
                vel.Linear += linearChange;
                vel.Angular += !body.VelocityData.IsRotationBlocked ? angularChange : float3.zero;

                body.VelocityData = vel;
                BodiesList[index] = body;
            }
        }
    }
}
