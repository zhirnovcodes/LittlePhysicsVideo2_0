using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace LittlePhysics
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ExportPhysicsDataSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var singleton = SystemAPI.GetSingleton<PhysicsSingleton>();
            var combinedDep = JobHandle.CombineDependencies(state.Dependency, singleton.PhysicsJobHandle);

            state.Dependency = new ExportPhysicsDataJob
            {
                BodiesList = singleton.BodiesList,
            }.ScheduleParallel(combinedDep);

            singleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(singleton);
        }

        [BurstCompile]
        private partial struct ExportPhysicsDataJob : IJobEntity
        {
            [ReadOnly] public NativeArray<PhysicsBodyData> BodiesList;

            public void Execute(Entity entity, ref LocalTransform transform, in PhysicsBodyComponent body, in PhysicsBodyUpdateComponent tag, ref PhysicsVelocityComponent velocity)
            {
                if (body.BodyType == BodyType.Dynamic == false)
                {
                    return;
                }

                if (!tag.IsEnabled)
                {
                    return;
                }

                var bodyData = BodiesList[tag.Index];
                transform.Position = bodyData.PositionData.Position - body.LocalPosition;
                transform.Rotation = math.mul(transform.Rotation, quaternion.EulerXYZ(bodyData.PositionData.RotationOffset));

                velocity.Linear = bodyData.VelocityData.Linear;
                velocity.Angular = bodyData.VelocityData.Angular;
            }
        }
    }
}
