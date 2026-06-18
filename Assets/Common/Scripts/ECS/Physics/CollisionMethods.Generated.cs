using Unity.Burst;
using Unity.Mathematics;

namespace LittlePhysics
{
    /// <summary>
    /// Carries the resolved physics response for one body in a collision pair:
    /// velocity changes to apply and the push-out correction vector.
    /// </summary>
    public struct BodyCollisionResult
    {
        public float3 LinearVelocityChange;
        public float3 AngularVelocityChange;
        public float3 PushOut;
    }

    [BurstCompile]
    public static partial class CollisionMethods
    {
        /// <summary>
        /// Computes collision resolution for both bodies in a pair.
        /// Dispatches to the appropriate per-shape implementation based on shape types.
        /// Unsupported shape pairs return default zero results.
        /// </summary>
        public static void ResolvePair(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            float3 contactPoint,
            out BodyCollisionResult result1,
            out BodyCollisionResult result2)
        {
            switch (body1.ShapeType, body2.ShapeType)
            {
                case (ShapeType.Sphere, ShapeType.Sphere):
                    Spheres.ResolvePair(body1, body2, contactPoint,
                        out result1, out result2);
                    return;
                default:
                    result1 = default;
                    result2 = default;
                    return;
            }
        }
    }
}
