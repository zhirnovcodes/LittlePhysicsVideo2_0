using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class BackgroundSpawnAuthoring : MonoBehaviour
{
    public Grid MapGrid;
    public Vector3Int GridCellCount;
    public float PairPositionOffset;
    public GameObject Prefab1;
    public GameObject Prefab2;

    private sealed class Baker : Baker<BackgroundSpawnAuthoring>
    {
        public override void Bake(BackgroundSpawnAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BackgroundSpawnComponent
            {
                GridStartPosition = authoring.MapGrid.transform.position,
                GridCellSize = authoring.MapGrid.cellSize,
                GridCellCount = new int3(authoring.GridCellCount.x, authoring.GridCellCount.y, authoring.GridCellCount.z),
                PairPositionOffset = authoring.PairPositionOffset,
                Prefab1 = GetEntity(authoring.Prefab1, TransformUsageFlags.Dynamic),
                Prefab2 = GetEntity(authoring.Prefab2, TransformUsageFlags.Dynamic),
            });
        }
    }
}
