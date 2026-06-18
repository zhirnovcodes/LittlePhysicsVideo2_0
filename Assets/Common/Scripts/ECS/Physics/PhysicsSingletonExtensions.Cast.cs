using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace LittlePhysics
{
    public struct LineCastResult
    {
        public Entity Target;
        public float3 Contact;
    }

    public struct CastFilter
    {
        [System.Flags]
        public enum BodyTypes
        {
            None = 0,
            Dynamic = 1 << 0,
            Static = 1 << 1,
            Trigger = 1 << 2
        }

        public BodyTypes Types;

        /// <summary>
        /// Source layer for layer-based collision filtering. -1 means all layers (no filtering).
        /// </summary>
        public int Layer;

        public static CastFilter Default => new CastFilter { Types = BodyTypes.Dynamic, Layer = -1 };
        public static CastFilter All => new CastFilter { Types = BodyTypes.Dynamic | BodyTypes.Static | BodyTypes.Trigger, Layer = -1 };
    }

    public static partial class PhysicsSingletonExtensions
    {
        /// <summary>
        /// Performs a line cast and returns the first (closest) hit against bodies matching the filter.
        /// Dynamic bodies (including dynamic triggers) are stored in DynamicMap.
        /// Static bodies (including static triggers) are stored in StaticMap.
        /// The filter's BodyTypes flags are matched against each body's IsDynamic / IsStatic / IsTrigger flags.
        /// When <see cref="CastFilter.Layer"/> >= 0, only bodies whose layer collides with the filter
        /// layer (as defined by <see cref="PhysicsSingleton.Settings"/>) are considered.
        /// </summary>
        public static bool LineCastFirst(
            this PhysicsSingleton physics,
            float3 start,
            float3 direction,
            CastFilter filter,
            out LineCastResult result)
        {
            result = default;
            float closestDistSq = float.MaxValue;
            bool found = false;

            bool shouldFindDynamic = (filter.Types & CastFilter.BodyTypes.Dynamic) != 0;
            bool shouldFindStatic = (filter.Types & CastFilter.BodyTypes.Static) != 0;
            bool shouldFindTrigger = (filter.Types & CastFilter.BodyTypes.Trigger) != 0;

            var line = new Line { Position = start, Direction = direction };
            var cellIterator = new TraverseLineIterator();

            while (physics.SpacialMap.TraverseLineNext(start, direction, ref cellIterator, out int cellId))
            {
                // Dynamic map: non-trigger dynamics + dynamic triggers
                if (shouldFindDynamic || shouldFindTrigger)
                {
                    var dynIt = new ListsArray<uint>.Iterator();
                    while (physics.CollisionMap.DynamicMap.Traverse(cellId, ref dynIt, out uint bodyIndex))
                    {
                        var body = physics.BodiesList[(int)bodyIndex];
                        bool matches = (shouldFindDynamic && body.IsDynamic) || (shouldFindTrigger && body.IsTrigger);
                        if (!matches) continue;
                        if (filter.Layer >= 0 && !physics.Settings.IsColliding(filter.Layer, body.Layer)) continue;
                        if (!CollisionMethods.IsLineCollidingBody(line, body, out float3 contact)) continue;
                        float distSq = math.distancesq(start, contact);
                        if (distSq < closestDistSq)
                        {
                            closestDistSq = distSq;
                            result = new LineCastResult { Target = body.Main, Contact = contact };
                            found = true;
                        }
                    }
                }

                // Static map: non-trigger statics + static triggers
                if (shouldFindStatic || shouldFindTrigger)
                {
                    var staticIt = new ListsArray<uint>.Iterator();
                    while (physics.CollisionMap.StaticMap.Traverse(cellId, ref staticIt, out uint bodyIndex))
                    {
                        var body = physics.BodiesList[(int)bodyIndex];
                        bool matches = (shouldFindStatic && body.IsStatic && !body.IsTrigger) || (shouldFindTrigger && body.IsTrigger && body.IsStatic);
                        if (!matches) continue;
                        if (filter.Layer >= 0 && !physics.Settings.IsColliding(filter.Layer, body.Layer)) continue;
                        if (!CollisionMethods.IsLineCollidingBody(line, body, out float3 contact)) continue;
                        float distSq = math.distancesq(start, contact);
                        if (distSq < closestDistSq)
                        {
                            closestDistSq = distSq;
                            result = new LineCastResult { Target = body.Main, Contact = contact };
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Performs a line cast and fills <paramref name="results"/> with all hits against bodies
        /// matching the filter. Returns the number of hits written.
        /// </summary>
        public static int LineCast(
            this PhysicsSingleton physics,
            float3 start,
            float3 direction,
            CastFilter filter,
            ref NativeArray<LineCastResult> results)
        {
            int count = 0;

            bool shouldFindDynamic = (filter.Types & CastFilter.BodyTypes.Dynamic) != 0;
            bool shouldFindStatic = (filter.Types & CastFilter.BodyTypes.Static) != 0;
            bool shouldFindTrigger = (filter.Types & CastFilter.BodyTypes.Trigger) != 0;

            var line = new Line { Position = start, Direction = direction };
            var cellIterator = new TraverseLineIterator();

            while (count < results.Length &&
                   physics.SpacialMap.TraverseLineNext(start, direction, ref cellIterator, out int cellId))
            {
                if (count < results.Length && (shouldFindDynamic || shouldFindTrigger))
                {
                    var dynIt = new ListsArray<uint>.Iterator();
                    while (count < results.Length &&
                           physics.CollisionMap.DynamicMap.Traverse(cellId, ref dynIt, out uint bodyIndex))
                    {
                        var body = physics.BodiesList[(int)bodyIndex];
                        bool matches = (shouldFindDynamic && body.IsDynamic) || (shouldFindTrigger && body.IsTrigger);
                        if (!matches) continue;
                        if (filter.Layer >= 0 && !physics.Settings.IsColliding(filter.Layer, body.Layer)) continue;
                        if (CollisionMethods.IsLineCollidingBody(line, body, out float3 contact))
                        {
                            results[count++] = new LineCastResult { Target = body.Main, Contact = contact };
                        }
                    }
                }

                if (count < results.Length && (shouldFindStatic || shouldFindTrigger))
                {
                    var staticIt = new ListsArray<uint>.Iterator();
                    while (count < results.Length &&
                           physics.CollisionMap.StaticMap.Traverse(cellId, ref staticIt, out uint bodyIndex))
                    {
                        var body = physics.BodiesList[(int)bodyIndex];
                        bool matches = (shouldFindStatic && body.IsStatic && !body.IsTrigger) || (shouldFindTrigger && body.IsTrigger && body.IsStatic);
                        if (!matches) continue;
                        if (filter.Layer >= 0 && !physics.Settings.IsColliding(filter.Layer, body.Layer)) continue;
                        if (CollisionMethods.IsLineCollidingBody(line, body, out float3 contact))
                        {
                            results[count++] = new LineCastResult { Target = body.Main, Contact = contact };
                        }
                    }
                }
            }

            SortLineCastResults(start, ref results, count);
            return count;
        }

        private static void SortLineCastResults(float3 origin, ref NativeArray<LineCastResult> results, int count)
        {
            for (int i = 1; i < count; i++)
            {
                LineCastResult key = results[i];
                float keyDistSq = math.distancesq(origin, key.Contact);
                int j = i - 1;

                while (j >= 0 && math.distancesq(origin, results[j].Contact) > keyDistSq)
                {
                    results[j + 1] = results[j];
                    j--;
                }

                results[j + 1] = key;
            }
        }
    }
}
