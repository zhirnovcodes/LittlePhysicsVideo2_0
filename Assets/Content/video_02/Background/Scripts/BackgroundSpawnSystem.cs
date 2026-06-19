using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct BackgroundSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BackgroundSpawnComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var spawnData = SystemAPI.GetSingleton<BackgroundSpawnComponent>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        SpawnAllCells(spawnData, ref ecb);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        state.Enabled = false;
    }

    private static void SpawnAllCells(BackgroundSpawnComponent spawnData, ref EntityCommandBuffer ecb)
    {
        for (int z = 0; z < spawnData.GridCellCount.z; z++)
        {
            for (int y = 0; y < spawnData.GridCellCount.y; y++)
            {
                for (int x = 0; x < spawnData.GridCellCount.x; x++)
                {
                    float3 cellCenter = spawnData.GridStartPosition
                        + (new float3(x, y, z) + 0.5f) * spawnData.GridCellSize;

                    float3 halfOffset = new float3(spawnData.PairPositionOffset * 0.5f, 0f, 0f);

                    var atom1 = ecb.Instantiate(spawnData.Prefab1);
                    ecb.SetComponent(atom1, LocalTransform.FromPosition(cellCenter - halfOffset));

                    var atom2 = ecb.Instantiate(spawnData.Prefab2);
                    ecb.SetComponent(atom2, LocalTransform.FromPosition(cellCenter + halfOffset));
                }
            }
        }
    }
}
