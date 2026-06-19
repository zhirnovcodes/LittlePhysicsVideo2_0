using Unity.Entities;
using Unity.Mathematics;

public struct BackgroundSpawnComponent : IComponentData
{
    public float3 GridStartPosition;
    public float3 GridCellSize;
    public int3 GridCellCount;
    public float PairPositionOffset;
    public Entity Prefab1;
    public Entity Prefab2;
}
