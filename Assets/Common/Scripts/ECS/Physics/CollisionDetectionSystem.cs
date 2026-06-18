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
    public partial struct CollisionDetectionSystem : ISystem
    {
        private ListsArray<uint> Pairs;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSingleton>();
            state.RequireForUpdate<LittlePhysicsTimeComponent>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (Pairs.IsCreated)
            {
                Pairs.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var physicsSingleton = SystemAPI.GetSingleton<PhysicsSingleton>();
            var collisionDataMap = physicsSingleton.Collisions.CollisionDataMap;
            
            int bodyCount = physicsSingleton.Settings.BlobRef.Value.LodData.MaxEntityCount;

            if (!Pairs.IsCreated)
            {
                int maxPairsPerBody = physicsSingleton.Settings.BlobRef.Value.LodData.MaxCollisionsPerEntity;
                Pairs = new ListsArray<uint>(bodyCount, maxPairsPerBody, Allocator.Persistent);
            }

            var physicsHandle = JobHandle.CombineDependencies(state.Dependency, physicsSingleton.PhysicsJobHandle);

            var clearJob = new ClearJob
            {
                CollisionDataMap = collisionDataMap,
                Pairs = Pairs,
            }.Schedule(physicsHandle);

            int maxCellsPerBody = physicsSingleton.Settings.BlobRef.Value.LodData.MaxCellPerEntity;

            bool checkVsDynamic = physicsSingleton.Settings.CheckSettings.CheckDynamicVsDynamic;
            bool checkVsStatic = physicsSingleton.Settings.CheckSettings.CheckDynamicVsStatic;

            JobHandle collectPairsJob;

            if (physicsSingleton.Settings.ShouldPairDetectByCells)
            {
                int cellCount = physicsSingleton.CollisionMap.DynamicMap.TotalListCount;

                collectPairsJob = new CollectPairsByCellsJob
                {
                    BodiesList = physicsSingleton.BodiesList,
                    DynamicMap = physicsSingleton.CollisionMap.DynamicMap,
                    StaticMap = physicsSingleton.CollisionMap.StaticMap,
                    Pairs = Pairs.AsParallelHashWriter(),
                    PhysicsSettings = physicsSingleton.Settings,
                    CheckVsDynamic = checkVsDynamic,
                    CheckVsStatic = checkVsStatic,
                }.Schedule(cellCount, 32, clearJob);
            }
            else
            {
                collectPairsJob = new CollectPairsByEntitiesJob
                {
                    BodiesList = physicsSingleton.BodiesList,
                    DynamicMap = physicsSingleton.CollisionMap.DynamicMap,
                    StaticMap = physicsSingleton.CollisionMap.StaticMap,
                    Pairs = Pairs,
                    PhysicsSettings = physicsSingleton.Settings,
                    CheckVsDynamic = checkVsDynamic,
                    CheckVsStatic = checkVsStatic,
                    MaxCellsPerBody = maxCellsPerBody,
                }.Schedule(bodyCount, 32, clearJob);
            }

            var detectCollisionsJob = new DetectCollisionsJob
            {
                BodiesList = physicsSingleton.BodiesList,
                Pairs = Pairs,
                CollisionDataWriter = collisionDataMap.AsParallelWriter(),
            }.Schedule(bodyCount, 32, collectPairsJob);

            state.Dependency = detectCollisionsJob;
            physicsSingleton.PhysicsJobHandle = state.Dependency;
            SystemAPI.SetSingleton(physicsSingleton);
        }

        [BurstCompile]
        private struct ClearJob : IJob
        {
            public ListsArray<CollisionData> CollisionDataMap;
            public ListsArray<uint> Pairs;

            public void Execute()
            {
                CollisionDataMap.Clear();
                Pairs.Clear();
            }
        }

        /// <summary>
        /// Per-body broad-phase. Traverses the body's AABB cells and writes canonical
        /// pairs (bodyIndex &lt; neighborIndex) into Pairs[bodyIndex]. The canonical guard
        /// ensures each pair is registered by exactly one side, halving work vs. the
        /// symmetric entity-based approach. The dedup scan prevents multi-cell
        /// re-encounters of the same neighbour from producing duplicate entries.
        /// Single-writer per list: job i writes only to Pairs[i].
        /// </summary>
        [BurstCompile]
        private struct CollectPairsByEntitiesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PhysicsBodyData> BodiesList;
            [ReadOnly] public ListsArray<uint> DynamicMap;
            [ReadOnly] public ListsArray<uint> StaticMap;
            [NativeDisableParallelForRestriction] public ListsArray<uint> Pairs;
            [ReadOnly] public PhysicsSettingsComponent PhysicsSettings;
            public bool CheckVsDynamic;
            public bool CheckVsStatic;
            public int MaxCellsPerBody;

            public void Execute(int bodyIndex)
            {
                var body = BodiesList[bodyIndex];

                if (body.Main == Entity.Null)
                {
                    return;
                }

                var aabb = body.MapUpdateData.CachedAABB;
                var cellIt = new AABBTraverseIterator(aabb);
                var random = new Random((uint)bodyIndex + 1u);

                while (TraverseCells(ref cellIt, MaxCellsPerBody, ref random, out int cellIndex))
                {
                    if (!Pairs.CanAdd(bodyIndex))
                    {
                        break;
                    }

                    if (CheckVsDynamic)
                    {
                        var dynIt = new ListsArray<uint>.Iterator();
                        while (DynamicMap.Traverse(cellIndex, ref dynIt, out uint neighborIndex))
                        {
                            if ((int)neighborIndex != bodyIndex)
                            {
                                TryAddPair(bodyIndex, (int)neighborIndex);
                            }
                        }
                    }

                    if (CheckVsStatic)
                    {
                        var staticIt = new ListsArray<uint>.Iterator();
                        while (StaticMap.Traverse(cellIndex, ref staticIt, out uint neighborIndex))
                        {
                            TryAddPair(bodyIndex, (int)neighborIndex);
                        }
                    }
                }
            }

            private void TryAddPair(int bodyIndex, int neighborIndex)
            {
                if (neighborIndex <= bodyIndex)
                {
                    return;
                }

                PhysicsDebug.SafeAssert((uint)neighborIndex < (uint)BodiesList.Length, "ImpulseDetectionSystem.TryAddPair: neighborIndex out of range");

                var bodyI = BodiesList[bodyIndex];
                var bodyJ = BodiesList[neighborIndex];

                if (bodyI.Main == bodyJ.Main)
                {
                    return;
                }

                if (!PhysicsSettings.IsColliding(bodyI.Layer, bodyJ.Layer))
                {
                    return;
                }

                if (!Pairs.CanAdd(bodyIndex))
                {
                    return;
                }

                int existingCount = Pairs.GetCount(bodyIndex);

                for (int slot = 0; slot < existingCount; slot++)
                {
                    if (Pairs.GetValue(bodyIndex, slot) == (uint)neighborIndex)
                    {
                        return;
                    }
                }

                Pairs.TryAdd(bodyIndex, (uint)neighborIndex);
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

        /// <summary>
        /// Cell-based broad-phase. Iterates every cell of the dynamic spatial map and
        /// generates canonical pairs (bodyIndex &lt; neighborIndex) for all body combinations
        /// sharing a cell. Uses ParallelHashWriter with a per-list spinlock so multiple
        /// threads can safely write to the same Pairs[bodyIndex] without duplicates.
        /// </summary>
        [BurstCompile]
        private struct CollectPairsByCellsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PhysicsBodyData> BodiesList;
            [ReadOnly] public ListsArray<uint> DynamicMap;
            [ReadOnly] public ListsArray<uint> StaticMap;
            [NativeDisableParallelForRestriction] public ListsArray<uint>.ParallelHashWriter Pairs;
            [ReadOnly] public PhysicsSettingsComponent PhysicsSettings;
            public bool CheckVsDynamic;
            public bool CheckVsStatic;

            public void Execute(int cellIndex)
            {
                if (CheckVsDynamic)
                {
                    CollectDynamicVsDynamic(cellIndex);
                }

                if (CheckVsStatic)
                {
                    CollectDynamicVsStatic(cellIndex);
                }
            }

            private void CollectDynamicVsDynamic(int cellIndex)
            {
                var outerIt = new ListsArray<uint>.Iterator();

                while (DynamicMap.Traverse(cellIndex, ref outerIt, out uint bodyIndex))
                {
                    var innerIt = new ListsArray<uint>.Iterator(outerIt.Index + 1);

                    while (DynamicMap.Traverse(cellIndex, ref innerIt, out uint neighborIndex))
                    {
                        TryAddPair((int)bodyIndex, (int)neighborIndex);
                    }
                }
            }

            private void CollectDynamicVsStatic(int cellIndex)
            {
                var outerIt = new ListsArray<uint>.Iterator();

                while (DynamicMap.Traverse(cellIndex, ref outerIt, out uint bodyIndex))
                {
                    var innerIt = new ListsArray<uint>.Iterator();

                    while (StaticMap.Traverse(cellIndex, ref innerIt, out uint neighborIndex))
                    {
                        TryAddPair((int)bodyIndex, (int)neighborIndex);
                    }
                }
            }

            private void TryAddPair(int bodyIndex, int neighborIndex)
            {
                if (neighborIndex <= bodyIndex)
                {
                    return;
                }

                var bodyI = BodiesList[bodyIndex];
                var bodyJ = BodiesList[neighborIndex];

                if (!PhysicsSettings.IsColliding(bodyI.Layer, bodyJ.Layer))
                {
                    return;
                }

                Pairs.TryAddUnique(bodyIndex, (uint)neighborIndex);
            }
        }

        /// <summary>
        /// Per-body narrow-phase. Reads canonical pairs from Pairs[bodyIndex] (i &lt; j,
        /// written by CollectPairsByEntitiesJob or CollectPairsByCellsJob which are fully
        /// complete before this job starts). For each confirmed collision both sides are
        /// written atomically to CollisionDataMap via ParallelWriter: body i gets
        /// impulse1/pushOut1, body j gets impulse2/pushOut2. No race on Pairs.GetCount
        /// because nothing writes to Pairs during this job.
        /// </summary>
        [BurstCompile]
        private struct DetectCollisionsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PhysicsBodyData> BodiesList;
            [ReadOnly] public ListsArray<uint> Pairs;
            [NativeDisableContainerSafetyRestriction] public ListsArray<CollisionData>.ParallelWriter CollisionDataWriter;

            public void Execute(int bodyIndex)
            {
                int pairCount = Pairs.GetCount(bodyIndex);

                if (pairCount == 0)
                {
                    return;
                }

                var bodyI = BodiesList[bodyIndex];

                for (int slot = 0; slot < pairCount; slot++)
                {
                    uint neighborIndex = Pairs.GetValue(bodyIndex, slot);

                    PhysicsDebug.SafeAssert(neighborIndex < (uint)BodiesList.Length, "DetectCollisionsJob: neighborIndex out of range");

                    var bodyJ = BodiesList[(int)neighborIndex];

                    if (!CollisionMethods.AreBodiesColliding(bodyI, bodyJ, out float3 contactPoint))
                    {
                        continue;
                    }

                    CollisionForces.GetCollisionImpulses(
                        bodyI, bodyJ, bodyI.VelocityData, bodyJ.VelocityData, contactPoint,
                        out float3 impulse1, out float3 impulse2);

                    CollisionForces.GetPushOutForce(
                        bodyI, bodyJ, contactPoint,
                        out float3 pushOut1, out float3 pushOut2);

                    CollisionDataWriter.TryAdd(bodyIndex, new CollisionData
                    {
                        Other = bodyJ.Main,
                        OtherIndex = neighborIndex,
                        ContactPoint = contactPoint,
                        Impulse = impulse1,
                        PushOut = pushOut1,
                    });

                    CollisionDataWriter.TryAdd((int)neighborIndex, new CollisionData
                    {
                        Other = bodyI.Main,
                        OtherIndex = (uint)bodyIndex,
                        ContactPoint = contactPoint,
                        Impulse = impulse2,
                        PushOut = pushOut2,
                    });
                }
            }
        }
    }
}
