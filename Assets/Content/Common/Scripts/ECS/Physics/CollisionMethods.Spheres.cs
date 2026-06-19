using Unity.Burst;
using Unity.Mathematics;

namespace LittlePhysics
{
    public static partial class CollisionMethods
    {
        [BurstCompile]
        public static class Spheres
        {
            private const float CollisionEpsilon = 0.001f;
            private const float CollisionEpsilonSq = CollisionEpsilon * CollisionEpsilon;

            /// <summary>
            /// Returns true when two sphere bodies overlap.
            /// Contact point is on the surface of body1 facing body2.
            /// </summary>
            public static bool AreBodiesColliding(
                in PhysicsBodyData body1,
                in PhysicsBodyData body2,
                out float3 contactPoint)
            {
                contactPoint = float3.zero;

                float3 centerDelta = body2.PositionData.Position - body1.PositionData.Position;
                float distanceSq = math.lengthsq(centerDelta);
                float combinedRadius = (body1.PositionData.Scale + body2.PositionData.Scale) * 0.5f;
                float combinedRadiusSq = combinedRadius * combinedRadius;

                bool colliding = distanceSq <= combinedRadiusSq + CollisionEpsilonSq && distanceSq > CollisionEpsilonSq;

                if (!colliding)
                {
                    return false;
                }

                float distance = math.sqrt(distanceSq);

                if (distance < 0.0001f)
                {
                    contactPoint = body1.PositionData.Position;
                }
                else
                {
                    float3 normal = centerDelta / distance;
                    contactPoint = body1.PositionData.Position + normal * (body1.PositionData.Scale * 0.5f);
                }

                return true;
            }

            /// <summary>
            /// Returns true when a ray intersects a sphere body.
            /// Contact point is at the entry point on the sphere surface.
            /// The ray only tests in the forward direction of line.Direction.
            /// </summary>
            public static bool IsLineCollidingBody(Line line, in PhysicsBodyData body, out float3 contactPoint)
            {
                contactPoint = float3.zero;

                float sphereRadius = body.PositionData.Scale * 0.5f;
                float directionLengthSq = math.lengthsq(line.Direction);

                if (directionLengthSq < 0.0001f)
                {
                    return false;
                }

                float3 normalizedDir = line.Direction * math.rsqrt(directionLengthSq);
                float3 toSphere = body.PositionData.Position - line.Position;
                float projection = math.dot(toSphere, normalizedDir);

                if (projection < 0f)
                {
                    return false;
                }

                float perpendicularDistSq = math.lengthsq(toSphere) - projection * projection;
                float radiusSq = sphereRadius * sphereRadius;

                if (perpendicularDistSq > radiusSq)
                {
                    return false;
                }

                float halfChord = math.sqrt(radiusSq - perpendicularDistSq);
                float hitDistance = projection - halfChord;

                if (hitDistance < 0f)
                {
                    hitDistance = projection + halfChord;
                }

                if (hitDistance < 0f)
                {
                    return false;
                }

                contactPoint = line.Position + normalizedDir * hitDistance;
                return true;
            }

            /// <summary>
            /// Computes linear velocity change, angular velocity change, and push-out correction
            /// for both sphere bodies at once. Trigger bodies or both-static pairs receive zero outputs.
            /// Dynamic-vs-dynamic and dynamic-vs-static are both handled.
            /// </summary>
            public static void ResolvePair(
                in PhysicsBodyData body1,
                in PhysicsBodyData body2,
                float3 contactPoint,
                out BodyCollisionResult result1,
                out BodyCollisionResult result2)
            {
                result1 = default;
                result2 = default;

                bool body1Dynamic = body1.IsDynamic;
                bool body2Dynamic = body2.IsDynamic;

                if (!body1Dynamic && !body2Dynamic)
                {
                    return;
                }

                float radius1 = body1.PositionData.Scale * 0.5f;
                float radius2 = body2.PositionData.Scale * 0.5f;
                float3 radiusVec1 = contactPoint - body1.PositionData.Position;
                float3 radiusVec2 = contactPoint - body2.PositionData.Position;
                float3 centerDelta = radiusVec1 - radiusVec2;
                float centerDistance = math.length(centerDelta);

                if (centerDistance < 0.0001f)
                {
                    return;
                }

                var vel1 = body1.VelocityData;
                var vel2 = body2.VelocityData;

                float3 normal = centerDelta / centerDistance;
                float sphereInertia1 = 0.4f * body1.RigidbodyData.Mass * radius1 * radius1;
                float sphereInertia2 = 0.4f * body2.RigidbodyData.Mass * radius2 * radius2;

                float3 velAtContact1 = vel1.Linear + math.cross(vel1.Angular, radiusVec1);
                float3 velAtContact2 = vel2.Linear + math.cross(vel2.Angular, radiusVec2);
                float relativeVelocity = math.dot(velAtContact2 - velAtContact1, normal);

                if (relativeVelocity < 0f)
                {
                    float3 angularCross1 = math.cross(radiusVec1, normal);
                    float3 angularCross2 = math.cross(radiusVec2, normal);
                    float angularEffect1 = body1Dynamic ? math.dot(angularCross1, angularCross1) / sphereInertia1 : 0f;
                    float angularEffect2 = body2Dynamic ? math.dot(angularCross2, angularCross2) / sphereInertia2 : 0f;
                    float inverseMassSum = (body1Dynamic ? 1f / body1.RigidbodyData.Mass : 0f)
                                        + (body2Dynamic ? 1f / body2.RigidbodyData.Mass : 0f);
                    float avgBounciness = (body1.RigidbodyData.Bounciness + body2.RigidbodyData.Bounciness) * 0.5f;
                    float impulseMagnitude = -(1f + avgBounciness) * relativeVelocity
                                           / (inverseMassSum + angularEffect1 + angularEffect2);
                    float3 impulseVector = normal * impulseMagnitude;

                    if (body1Dynamic)
                    {
                        result1.LinearVelocityChange = -impulseVector / body1.RigidbodyData.Mass;
                        result1.AngularVelocityChange = math.cross(radiusVec1, -impulseVector) / sphereInertia1;
                    }

                    if (body2Dynamic)
                    {
                        result2.LinearVelocityChange = impulseVector / body2.RigidbodyData.Mass;
                        result2.AngularVelocityChange = math.cross(radiusVec2, impulseVector) / sphereInertia2;
                    }
                }

                if (body1.RigidbodyData.Hardness == 0f && body2.RigidbodyData.Hardness == 0f)
                {
                    return;
                }

                float penetrationDepth = (radius1 + radius2) - centerDistance;

                if (penetrationDepth <= 0f)
                {
                    return;
                }

                const float MinPushPower = 0.02f;
                const float MaxPushPower = 0.5f;

                if (body1Dynamic)
                {
                    result1.PushOut = -normal * penetrationDepth * math.lerp(MinPushPower, MaxPushPower, body2.RigidbodyData.Hardness);
                }

                if (body2Dynamic)
                {
                    result2.PushOut = normal * penetrationDepth * math.lerp(MinPushPower, MaxPushPower, body1.RigidbodyData.Hardness);
                }
            }

            /// <summary>
            /// Returns the outward surface normal of the sphere body at the given contact point.
            /// </summary>
            public static float3 GetNormal(in PhysicsBodyData body, float3 contactPoint)
            {
                float3 direction = contactPoint - body.PositionData.Position;
                float directionLength = math.length(direction);

                if (directionLength < 0.0001f)
                {
                    return new float3(0f, 1f, 0f);
                }

                return direction / directionLength;
            }

            /// <summary>
            /// Returns the axis-aligned bounding box for a sphere.
            /// </summary>
            public static Rectangle GetRectangle(PositionData pos)
            {
                return new Rectangle
                {
                    Position = pos.Position,
                    Scale = new float3(pos.Scale, pos.Scale, pos.Scale),
                };
            }
        }
    }
}
