using Unity.Mathematics;

namespace LittlePhysics
{
    public static partial class SpacialMapExtensions
    {
        /// <summary>
        /// Checks if a world position is within the grid bounds
        /// </summary>
        public static bool IsInGrid(this SpacialMap spacialMap, float3 position)
        {
            int3 cell = spacialMap.Grid.GetCell(position);

            return cell.x >= 0 && cell.x < spacialMap.GridSize.x &&
                   cell.y >= 0 && cell.y < spacialMap.GridSize.y &&
                   cell.z >= 0 && cell.z < spacialMap.GridSize.z;
        }

        /// <summary>
        /// Converts a linear cell index to cell coordinates
        /// </summary>
        public static int3 IndexToCell(this SpacialMap spacialMap, int index)
        {
            int3 gridSize = spacialMap.GridSize;
            int z = index / (gridSize.x * gridSize.y);
            int y = (index % (gridSize.x * gridSize.y)) / gridSize.x;
            int x = index % gridSize.x;
            return new int3(x, y, z);
        }

        /// <summary>
        /// Gets the world position of the cell center at the given linear index
        /// </summary>
        public static float3 GetCellPosition(this SpacialMap spacialMap, int index)
        {
            var cell = spacialMap.IndexToCell(index);
            return spacialMap.Grid.GetCellPosition(cell);
        }

        /// <summary>
        /// Checks if a cell coordinate is within the grid bounds
        /// </summary>
        public static bool IsInBounds(this SpacialMap spacialMap, int3 cell)
        {
            return cell.x >= 0 && cell.x < spacialMap.GridSize.x &&
                   cell.y >= 0 && cell.y < spacialMap.GridSize.y &&
                   cell.z >= 0 && cell.z < spacialMap.GridSize.z;
        }
    }
}
