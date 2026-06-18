using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace LittlePhysics
{
    [BurstCompile]
    [UpdateInGroup(typeof(LittlePhysicsInternalSystemGroup), OrderFirst = true)]
    public partial struct CollisionMapUpdateSystem : ISystem
    {

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSingleton>();
            state.RequireForUpdate<LittlePhysicsTimeComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var physicsSingleton = SystemAPI.GetSingleton<PhysicsSingleton>();
            var time = SystemAPI.GetSingleton<LittlePhysicsTimeComponent>();
            var physicsSettings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();

            var dynamicMap = physicsSingleton.CollisionMap.DynamicMap;
            var staticMap = physicsSingleton.CollisionMap.StaticMap;

            var physicsHandle = JobHandle.CombineDependencies(state.Dependency, physicsSingleton.PhysicsJobHandle);

            var clearJob = new ClearJob
            {
                DynamicMap = dynamicMap,
                StaticMap = staticMap,
            }.Schedule(physicsHandle);

            var buildAABBCacheJob = new BuildAABBCacheJob
            {
                SpacialMap = physicsSingleton.SpacialMap,
                BodiesList = physicsSingleton.BodiesList,
            }.Schedule(physicsSingleton.BodiesList.Length, 16, physicsHandle);

            var maxCellPerEntity = physicsSettings.BlobRef.Value.LodData.MaxCellPerEntity;

            switch (time.TimeScale)
            {
                case 2:
                    maxCellPerEntity = physicsSettings.BlobRef.Value.LodData.MaxCellPerEntityX2;
                    break;
                case 4:
                    maxCellPerEntity = physicsSettings.BlobRef.Value.LodData.MaxCellPerEntityX4;
                    break;
            }

            var prerequisiteHandle = JobHandle.CombineDependencies(clearJob, buildAABBCacheJob);

            var addBodiesJob = new AddBodiesJob
            {
                BodiesList = physicsSingleton.BodiesList,
                DynamicMap = dynamicMap.AsParallelWriter(),
                StaticMap = staticMap.AsParallelWriter(),
                Randoms = physicsSingleton.Randoms,
                MaxCellsPerEntity = maxCellPerEntity,
            }.Schedule(physicsSingleton.BodiesList.Length, 16, prerequisiteHandle);

            state.Dependency = addBodiesJob;

            physicsSingleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(physicsSingleton);
        }

        [BurstCompile]
        private struct ClearJob : IJob
        {
            [NativeDisableParallelForRestriction] public ListsArray<uint> DynamicMap;
            [NativeDisableParallelForRestriction] public ListsArray<uint> StaticMap;

            public void Execute()
            {
                DynamicMap.Clear();
                //StaticMap.Clear();
            }
        }

        [BurstCompile]
        private struct BuildAABBCacheJob : IJobParallelFor
        {
            [ReadOnly] public SpacialMap SpacialMap;
            [NativeDisableParallelForRestriction] public NativeArray<PhysicsBodyData> BodiesList;

            public void Execute(int index)
            {
                var body = BodiesList[index];

                if (body.Main == Entity.Null)
                {
                    return;
                }

                body.MapUpdateData.CachedAABB = MapExtensions.GetAABB(SpacialMap, body.ShapeType, body.PositionData);
                BodiesList[index] = body;
            }
        }

        [BurstCompile]
        private struct AddBodiesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PhysicsBodyData> BodiesList;

            [NativeDisableParallelForRestriction] public ListsArray<uint>.ParallelWriter DynamicMap;
            [NativeDisableParallelForRestriction] public ListsArray<uint>.ParallelWriter StaticMap;
            [NativeDisableParallelForRestriction] public NativeArray<Random> Randoms;

            public int MaxCellsPerEntity;

            public void Execute(int index)
            {
                var body = BodiesList[index];

                if (body.Main == Entity.Null)
                {
                    return;
                }

                if (!body.MapUpdateData.ShouldUpdate)
                {
                    return;
                }

                if (body.IsStatic)
                {
                    AddBodyToStatic(index, body);
                }
                else
                {
                    AddBodyToDynamic(index, body);
                }
            }

            private void AddBodyToDynamic(int index, PhysicsBodyData body)
            {
                var aabb = body.MapUpdateData.CachedAABB;
                var iterator = new AABBTraverseIterator(aabb);
                var random = Randoms[index];

                while (TraverseCells(ref iterator, MaxCellsPerEntity, ref random, out int cellIndex))
                {
                    DynamicMap.TryAdd(cellIndex, (uint)index);
                }

                Randoms[index] = random;
            }

            private void AddBodyToStatic(int index, PhysicsBodyData body)
            {
                var aabb = body.MapUpdateData.CachedAABB;
                var iterator = new AABBTraverseIterator(aabb);

                while (MapExtensions.Traverse(ref iterator, out int cellIndex))
                {
                    StaticMap.TryAdd(cellIndex, (uint)index);
                }
            }

            private static bool TraverseCells(
                ref AABBTraverseIterator it,
                int maxCells,
                ref Random random,
                out int cellIndex)
            {
                if (it.CellsCount > maxCells)
                {
                    return MapExtensions.TraverseOptimised(ref it, ref random, maxCells, out cellIndex);
                }

                return MapExtensions.Traverse(ref it, out cellIndex);
            }
        }
    }
}
