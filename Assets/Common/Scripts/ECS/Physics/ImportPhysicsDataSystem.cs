using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace LittlePhysics
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ImportPhysicsDataSystem : ISystem
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

            var settings2 = SystemAPI.GetSingleton<PhysicsSettingsComponent>();
            var lodSettings = settings2.BlobRef.Value.LodData;
            var lodMax = lodSettings.MaxEntityCount;
            var rootMax = settings2.BlobRef.Value.MaxEntitiesCount;
            var maxEntitiesCount = lodMax > 0 ? lodMax : rootMax;

            var combinedDep = JobHandle.CombineDependencies(state.Dependency, singleton.PhysicsJobHandle);

            var clearJob = new ClearBodiesJob
            {
                BodiesList = singleton.BodiesList,
            }.Schedule(combinedDep);

            var importJob = new ImportPhysicsDataJob
            {
                BodiesList = singleton.BodiesList,
                MaxEntitiesCount = maxEntitiesCount,
                MaxBodiesPerLod = maxEntitiesCount,
                DeltaTime = SystemAPI.Time.DeltaTime,
                VelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocityComponent>(true),
            }.Schedule(clearJob);

            state.Dependency = importJob;

            singleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(singleton);
        }
    }

    [BurstCompile]
    public partial struct ClearBodiesJob : IJob
    {
        [NativeDisableParallelForRestriction] public NativeArray<PhysicsBodyData> BodiesList;

        public void Execute()
        {
            unsafe
            {
                void* ptr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(BodiesList);
                long sizeInBytes = (long)BodiesList.Length * UnsafeUtility.SizeOf<PhysicsBodyData>();

                UnsafeUtility.MemClear(ptr, sizeInBytes);
            }
        }
    }
    /*
    [BurstCompile]
    public partial struct ComputePhysicsLodJob : IJobEntity
    {
        public CameraData Camera;
        public float2 DistanceRange;

        public void Execute(in LocalToWorld localToWorld, in PhysicsBodyComponent body, ref PhysicsBodyUpdateComponent tag)
        {
            float3 worldPos = localToWorld.Position + body.LocalPosition;
            float dist = math.distance(Camera.CameraPosition, worldPos);
            bool inDist = dist >= DistanceRange.x && dist <= DistanceRange.y;

            float4 clip = math.mul(Camera.WorldToClipMatrix, new float4(worldPos, 1f));
            float invW = math.rcp(clip.w);
            float3 ndc = clip.xyz * invW;
            bool inVp = ndc.x >= -1f && ndc.x <= 1f && ndc.y >= -1f && ndc.y <= 1f && ndc.z >= -1f && ndc.z <= 1f;

            tag.LodIndex = (inDist && inVp) ? 1 : 0;
        }
    }*/

    [BurstCompile]
    public partial struct ImportPhysicsDataJob : IJobEntity
    {
        public NativeArray<PhysicsBodyData> BodiesList;
        public int MaxEntitiesCount;
        public int MaxBodiesPerLod;
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<PhysicsVelocityComponent> VelocityLookup;

        public void Execute(
            [EntityIndexInQuery] int entityInQueryIndex,
            Entity entity,
            in LocalToWorld localToWorld,
            in PhysicsBodyComponent body,
            ref PhysicsBodyUpdateComponent tag)
        {
            if (entityInQueryIndex >= MaxEntitiesCount)
            {
                tag.IsEnabled = false;
                return;
            }

            int lod = tag.LodIndex;

            bool shouldUpdateMap = false;

            switch (tag.Type)
            {
                case UpdateType.EveryFrame:
                    shouldUpdateMap = true;
                    break;
                case UpdateType.Once:
                    if (tag.WasUpdated == false)
                    {
                        tag.WasUpdated = true;
                        shouldUpdateMap = true;
                    }
                    break;
                case UpdateType.WithInterval:
                    if (tag.WasUpdated)
                    {
                        shouldUpdateMap = (int)math.floor(tag.TimeElapsed / tag.Interval) != (int)math.floor((tag.TimeElapsed - DeltaTime) / tag.Interval);
                    }
                    else
                    {
                        tag.WasUpdated = true;
                        shouldUpdateMap = true;
                    }

                    tag.TimeElapsed += DeltaTime;
                    break;
            }

            tag.IsEnabled = true;

            var bodyData = body.ToBodyData(localToWorld, tag.LodIndex, shouldUpdateMap);
            tag.Index = entityInQueryIndex;

            if (VelocityLookup.TryGetComponent(entity, out var velComp))
            {
                bodyData.VelocityData = velComp.ToVelocityData();
            }

            bodyData.VelocityData.IsRotationBlocked = !body.ShouldRotateOnCollision;

            BodiesList[entityInQueryIndex] = bodyData;
        }
    }
}
