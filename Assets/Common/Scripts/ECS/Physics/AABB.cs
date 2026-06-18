using Unity.Mathematics;

namespace LittlePhysics
{
    public struct AABB
    {
        public int3 Min;
        public int3 Max;
        public int3 MapSize;

        public int ToGlobalIndex(int localIndex)
        {
            int3 size = Max - Min + 1;
            int lz = localIndex / (size.x * size.y);
            int rem = localIndex % (size.x * size.y);
            int ly = rem / size.x;
            int lx = rem % size.x;
            int3 cell = Min + new int3(lx, ly, lz);
            return cell.z * (MapSize.x * MapSize.y) + cell.y * MapSize.x + cell.x;
        }

        public int GetCellsCount()
        {
            int3 size = Max - Min + 1;
            return size.x * size.y * size.z;
        }

        /// <summary>
        /// Returns the flat cell index of the overlap min-corner of a and b's cell bounds.
        /// Both bodies' threads agree on this single canonical cell, providing O(1) pair dedup
        /// without needing a shared hash map or atomic compare-exchange.
        /// </summary>
        public static int CanonicalCell(AABB a, AABB b)
        {
            int3 overlapMin = math.max(a.Min, b.Min);
            return overlapMin.z * (a.MapSize.x * a.MapSize.y) + overlapMin.y * a.MapSize.x + overlapMin.x;
        }

        public static bool AreIntersect(AABB a, AABB b)
        {
            if (a.Min.x > b.Max.x)
            {
                return false;
            }
            if (b.Min.x > a.Max.x)
            {
                return false;
            }
            if (a.Min.y > b.Max.y)
            {
                return false;
            }
            if (b.Min.y > a.Max.y)
            {
                return false;
            }
            if (a.Min.z > b.Max.z)
            {
                return false;
            }
            if (b.Min.z > a.Max.z)
            {
                return false;
            }
            return true;
        }
    }
}
