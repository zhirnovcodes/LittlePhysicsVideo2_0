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
    [UpdateBefore(typeof(PhysicsVelocitySystem))]
    public partial struct GravitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSingleton>();
            state.RequireForUpdate<PhysicsSettingsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<PhysicsSingleton>();
            var settings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();
            int bodyCount = settings.BlobRef.Value.LodData.MaxEntityCount;

            var dep = JobHandle.CombineDependencies(state.Dependency, singleton.PhysicsJobHandle);

            if (SystemAPI.HasSingleton<SphericalGravitySourceComponent>())
            {
                dep = new SphericalGravityJob
                {
                    Source = SystemAPI.GetSingleton<SphericalGravitySourceComponent>(),
                    BodiesList = singleton.BodiesList,
                }.Schedule(bodyCount, 32, dep);
            }

            if (SystemAPI.HasSingleton<DirectionalGravitySourceComponent>())
            {
                dep = new DirectionalGravityJob
                {
                    Source = SystemAPI.GetSingleton<DirectionalGravitySourceComponent>(),
                    BodiesList = singleton.BodiesList,
                }.Schedule(bodyCount, 32, dep);
            }

            state.Dependency = dep;
            singleton.PhysicsJobHandle = dep;
            SystemAPI.SetSingleton(singleton);
        }

        [BurstCompile]
        private struct SphericalGravityJob : IJobParallelFor
        {
            [ReadOnly] public SphericalGravitySourceComponent Source;
            [NativeDisableContainerSafetyRestriction] public NativeArray<PhysicsBodyData> BodiesList;

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

                float3 toSource = Source.Center - body.PositionData.Position;
                float distance = math.length(toSource);

                if (distance < 0.001f)
                {
                    return;
                }

                float3 direction = toSource / distance;
                float gravityMagnitude = Source.SurfaceGravity * (Source.Radius * Source.Radius) / (distance * distance);

                body.VelocityData.Linear += direction * gravityMagnitude;
                BodiesList[index] = body;
            }
        }

        [BurstCompile]
        private struct DirectionalGravityJob : IJobParallelFor
        {
            [ReadOnly] public DirectionalGravitySourceComponent Source;
            [NativeDisableContainerSafetyRestriction] public NativeArray<PhysicsBodyData> BodiesList;

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

                float surfaceEdge = Source.IsUp
                    ? CollisionForces.GetHighestPointY(body)
                    : CollisionForces.GetLowestPointY(body);

                bool atSurface = Source.IsUp
                    ? surfaceEdge >= Source.SurfaceY
                    : surfaceEdge <= Source.SurfaceY;

                if (atSurface)
                {
                    return;
                }

                float3 direction = Source.IsUp ? new float3(0f, 1f, 0f) : new float3(0f, -1f, 0f);

                body.VelocityData.Linear += direction * Source.Strength;
                BodiesList[index] = body;
            }
        }
    }
}
