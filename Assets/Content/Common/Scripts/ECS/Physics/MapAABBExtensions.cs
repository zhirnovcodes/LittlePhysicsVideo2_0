using Unity.Mathematics;

namespace LittlePhysics
{
    public struct AABBTraverseIterator
    {
        public AABB Aabb;
        public int3 Index3d;   // current local (x, y, z) offset from Aabb.Min — used by Traverse
        public int Index;      // flat local index — used by TraverseOptimised
        public bool IsStarted;
        public int CellsCount; // cached result of Aabb.GetCellsCount(), set at creation

        public AABBTraverseIterator(AABB aabb)
        {
            Aabb = aabb;
            CellsCount = aabb.GetCellsCount();
            Index3d = int3.zero;
            Index = 0;
            IsStarted = false;
        }
    }

    public static class MapExtensions
    {
        /// <summary>
        /// Computes the cell-space AABB that covers the bounding box of the given shape
        /// at the given position within the spatial map.
        /// </summary>
        public static AABB GetAABB(SpacialMap map, ShapeType shapeType, PositionData pos)
        {
            Rectangle rect = ShapeExtensions.GetRectangle(shapeType, pos);
            float3 halfExtents = rect.Scale * 0.5f;
            float3 minWorld = rect.Position - halfExtents;
            float3 maxWorld = rect.Position + halfExtents;

            int3 minCell = map.Grid.GetCell(minWorld);
            int3 maxCell = map.Grid.GetCell(maxWorld);

            minCell = math.clamp(minCell, int3.zero, map.GridSize - 1);
            maxCell = math.clamp(maxCell, int3.zero, map.GridSize - 1);

            return new AABB
            {
                Min = minCell,
                Max = maxCell,
                MapSize = map.GridSize
            };
        }

        /// <summary>
        /// Iterates every cell covered by the iterator's AABB in linear order.
        /// Uses Index3d incremented with carry-propagation to avoid integer divisions
        /// on every step. Returns false when all cells have been visited.
        /// </summary>
        public static bool Traverse(ref AABBTraverseIterator iterator, out int cellIndex)
        {
            int3 size = iterator.Aabb.Max - iterator.Aabb.Min + 1;

            if (!iterator.IsStarted)
            {
                iterator.IsStarted = true;
                iterator.Index3d = int3.zero;
            }
            else
            {
                iterator.Index3d.x++;
                if (iterator.Index3d.x >= size.x)
                {
                    iterator.Index3d.x = 0;
                    iterator.Index3d.y++;
                    if (iterator.Index3d.y >= size.y)
                    {
                        iterator.Index3d.y = 0;
                        iterator.Index3d.z++;
                    }
                }
            }

            if (iterator.Index3d.z >= size.z)
            {
                cellIndex = -1;
                return false;
            }

            int3 cell = iterator.Aabb.Min + iterator.Index3d;
            cellIndex = cell.z * (iterator.Aabb.MapSize.x * iterator.Aabb.MapSize.y)
                      + cell.y * iterator.Aabb.MapSize.x
                      + cell.x;
            return true;
        }

        /// <summary>
        /// Iterates a random-sampled subset of cells from the iterator's AABB.
        /// Step size is chosen randomly in [minStep, maxStep] where the range is
        /// derived from totalCells / maxCount, matching the existing optimised
        /// traverse pattern. Returns false when sampling is exhausted.
        /// </summary>
        public static bool TraverseOptimised(
            ref AABBTraverseIterator iterator,
            ref Random random,
            int maxCount,
            out int cellIndex)
        {
            int totalCells = iterator.CellsCount;
            int maxIndex = totalCells - 1;

            int maxStep = math.max(1, (int)math.ceil((float)totalCells / (float)maxCount));
            int minStep = math.max(1, maxStep - 1);

            if (!iterator.IsStarted)
            {
                int startOffset = random.NextInt(0, maxStep + 1);
                startOffset = math.min(startOffset, maxIndex);
                iterator.IsStarted = true;
                iterator.Index = startOffset;
                cellIndex = iterator.Aabb.ToGlobalIndex(iterator.Index);
                return true;
            }

            int remainingIndices = maxIndex - iterator.Index;

            if (remainingIndices <= 0)
            {
                cellIndex = -1;
                return false;
            }

            int clampedMaxStep = math.min(maxStep, remainingIndices);
            int clampedMinStep = math.min(minStep, clampedMaxStep);
            int stepRange = clampedMaxStep - clampedMinStep + 1;
            int randomStep = clampedMinStep + random.NextInt(0, stepRange);

            iterator.Index += randomStep;

            if (iterator.Index > maxIndex)
            {
                cellIndex = -1;
                return false;
            }

            cellIndex = iterator.Aabb.ToGlobalIndex(iterator.Index);
            return true;
        }
    }
}
