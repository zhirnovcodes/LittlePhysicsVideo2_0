using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace LittlePhysics
{
    /// <summary>
    /// Runs once in InitializationSystemGroup. Waits until PhysicsSettingsComponent is ready,
    /// then allocates ALL physics native containers, creates PhysicsSingleton (which holds
    /// references to every container), and disables itself.
    ///
    /// This system is the single owner of every Persistent allocation listed below.
    /// Systems access containers via PhysicsSingleton — no direct injection needed.
    /// All systems that use these containers must NOT dispose them; this system disposes
    /// everything in OnDestroy.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct LittlePhysicsBootstrapSystem : ISystem
    {
        private NativeArray<PhysicsBodyData> BodiesList;
        private ListsArray<uint> DynamicMap;
        private ListsArray<uint> StaticMap;
        private ListsArray<CollisionData> CollisionDataMap;
        private NativeArray<SurfaceCollisionData> SurfaceCollisionMap;
        private NativeArray<Unity.Mathematics.Random> Randoms;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsSettingsInitComponent>();
            state.RequireForUpdate<SpacialMapSettingsComponent>();
            state.RequireForUpdate<PhysicsMapRandomComponent>();
        }

        public void OnDestroy(ref SystemState state)
        {
            if (BodiesList.IsCreated) BodiesList.Dispose();
            if (DynamicMap.IsCreated) DynamicMap.Dispose();
            if (StaticMap.IsCreated) StaticMap.Dispose();
            if (CollisionDataMap.IsCreated) CollisionDataMap.Dispose();
            if (SurfaceCollisionMap.IsCreated) SurfaceCollisionMap.Dispose();
            if (Randoms.IsCreated) Randoms.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!WaitForSettings(ref state))
                return;

            CreateDataStructures(ref state);
            CreateSingleton(ref state);

            state.Enabled = false;
        }

        // -------------------------------------------------------------------------
        // Settings

        private bool WaitForSettings(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<PhysicsSettingsComponent>() == false)
            {
                CreatePhysicsSettings(ref state);
                return false;
            }

            return true;
        }

        private void CreatePhysicsSettings(ref SystemState state)
        {
            var settingsData = SystemAPI.GetSingleton<PhysicsSettingsInitComponent>();

            var layersMapsArray = new NativeArray<int>(32, Allocator.Temp);
            for (int layer = 0; layer < 32; layer++)
            {
                int layerMask = 0;
                for (int otherLayer = 0; otherLayer < 32; otherLayer++)
                {
                    if (!Physics.GetIgnoreLayerCollision(layer, otherLayer))
                        layerMask |= (1 << otherLayer);
                }
                layersMapsArray[layer] = layerMask;
            }

            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<PhysicsSettingsBlobAsset>();
            root.MaxEntitiesCount = settingsData.MaxEntitiesCount;
            root.LodData = settingsData.LodData;
            root.EnvironmentSettings = settingsData.EnvironmentSettings;

            var layersMapsBuilder = builder.Allocate(ref root.LayersMaps, 32);
            for (int i = 0; i < 32; i++)
            {
                layersMapsBuilder[i] = layersMapsArray[i];
            }

            layersMapsArray.Dispose();

            var blobRef = builder.CreateBlobAssetReference<PhysicsSettingsBlobAsset>(Allocator.Persistent);
            builder.Dispose();

            var spacialMap = SystemAPI.GetSingleton<SpacialMapSettingsComponent>().SpacialMap;
            bool shouldPairDetectByCells = GetPairDetectByCell(settingsData, spacialMap);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new PhysicsSettingsComponent
            {
                BlobRef = blobRef,
                CheckSettings = settingsData.CheckSettings,
                ShouldPairDetectByCells = shouldPairDetectByCells
            });
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            Debug.Log("[LittlePhysicsBootstrap] CreatePhysicsSettings: PhysicsSettingsComponent created");
        }

        private static bool GetPairDetectByCell(PhysicsSettingsInitComponent settingsData, SpacialMap spacialMap)
        {
            const float lockCoeff = 2f;

            int totalCells = spacialMap.GetCellsCount();
            int maxEntities = settingsData.MaxEntitiesCount;
            var maxEntitiesInCells = math.max(settingsData.LodData.MaxDynamicsInCells, settingsData.LodData.MaxStaticInCells);
            var maxCellsPerEntity = settingsData.LodData.MaxCellPerEntity;

            var shouldPairDetectByCells = maxEntities * maxCellsPerEntity > totalCells * maxEntitiesInCells * lockCoeff;

            Debug.Log("[LittlePhysicsBootstrap] WaitForSettings: ShouldPairDetectByCells is set to " + shouldPairDetectByCells);
            return shouldPairDetectByCells;
        }

        // -------------------------------------------------------------------------
        // Allocation

        private void CreateDataStructures(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();
            ref var blob = ref settings.BlobRef.Value;
            ref var lod = ref blob.LodData;

            var spacialMapSingleton = SystemAPI.GetSingleton<SpacialMapSettingsComponent>();
            var randomComponent = SystemAPI.GetSingleton<PhysicsMapRandomComponent>();

            int3 gridSize = spacialMapSingleton.SpacialMap.GridSize;
            int totalCells = gridSize.x * gridSize.y * gridSize.z;
            int maxEntities = lod.MaxEntityCount;

            BodiesList = new NativeArray<PhysicsBodyData>(maxEntities, Allocator.Persistent);
            DynamicMap = new ListsArray<uint>(totalCells, lod.MaxDynamicsInCells, Allocator.Persistent);
            StaticMap = new ListsArray<uint>(totalCells, lod.MaxStaticInCells, Allocator.Persistent);
            CollisionDataMap = new ListsArray<CollisionData>(maxEntities, lod.MaxCollisionsPerEntity, Allocator.Persistent);
            SurfaceCollisionMap = new NativeArray<SurfaceCollisionData>(maxEntities, Allocator.Persistent);

            Randoms = new NativeArray<Unity.Mathematics.Random>(maxEntities, Allocator.Persistent);
            for (int i = 0; i < maxEntities; i++)
            {
                Randoms[i] = new Unity.Mathematics.Random(randomComponent.Seed + (uint)i + 1u);
            }

            Debug.Log("[LittlePhysicsBootstrap] Allocate: all containers allocated.");
        }

        // -------------------------------------------------------------------------
        // Singleton creation

        private void CreateSingleton(ref SystemState state)
        {
            var spacialMap = SystemAPI.GetSingleton<SpacialMapSettingsComponent>().SpacialMap;
            var settings = SystemAPI.GetSingleton<PhysicsSettingsComponent>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var singletonEntity = ecb.CreateEntity();
            ecb.AddComponent(singletonEntity, new PhysicsSingleton
            {
                BodiesList = BodiesList,
                Randoms = Randoms,
                CollisionMap = new CollisionMapSingleton
                {
                    DynamicMap = DynamicMap,
                    StaticMap = StaticMap,
                    SurfaceCollisionMap = SurfaceCollisionMap,
                },
                Collisions = new CollisionsSingleton
                {
                    CollisionDataMap = CollisionDataMap,
                },
                SpacialMap = spacialMap,
                Settings = settings,
            });
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            Debug.Log("[LittlePhysicsBootstrap] CreateSingleton: PhysicsSingleton created; bootstrap will disable.");
        }
    }
}
