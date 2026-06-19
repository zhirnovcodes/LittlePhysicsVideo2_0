using Unity.Burst;
using Unity.Mathematics;

namespace LittlePhysics
{
    [BurstCompile]
    public static class CollisionForces
    {
        /// <summary>
        /// Returns the radius vector from the body's effective rotation center to the contact point.
        /// Sphere / ReverseSphere: from sphere center to contact point.
        /// Capsule: from the nearest point on the capsule's core axis segment to the contact point
        ///          (perpendicular from axis to surface).
        /// SimplePlane: zero (no rotation).
        /// </summary>
        public static float3 GetRadiusVector(in PhysicsBodyData body, float3 contactPoint)
        {
            switch (body.ShapeType)
            {
                case ShapeType.Capsule:
                {
                    float3 capA = body.PositionData.Position - body.PositionData.Up * 0.5f;
                    float3 capB = body.PositionData.Position + body.PositionData.Up * 0.5f;
                    float3 axisPoint = closestPointOnSegment(capA, capB, contactPoint);
                    return contactPoint - axisPoint;
                }
                case ShapeType.SimplePlane:
                    return float3.zero;
                case ShapeType.ReverseSphere:
                    return -contactPoint + body.PositionData.Position;
                default:
                    return contactPoint - body.PositionData.Position;
            }
        }

        private static float3 GetDelta(in PhysicsBodyData body1, in PhysicsBodyData body2, float3 contactPoint)
        {
            return GetRadiusVector(body1, contactPoint) - GetRadiusVector(body2, contactPoint);
        }

        /// <summary>
        /// Returns the vector from the body's center of mass to the contact point.
        /// Used for all dynamics calculations (torque, velocity at contact, angular impulse denominator).
        /// This is distinct from GetRadiusVector, which is used only for computing the collision normal.
        /// SimplePlane returns zero because planes do not rotate.
        /// </summary>
        private static float3 GetDynamicsRadius(in PhysicsBodyData body, float3 contactPoint)
        {
            if (body.ShapeType == ShapeType.SimplePlane)
            {
                return float3.zero;
            }
            return contactPoint - body.PositionData.Position;
        }

        private static void GetCapsuleInertia(
            float mass,
            float radius,
            float halfCylinderHeight,
            out float axialInertia,
            out float lateralInertia)
        {
            float r  = radius;
            float h  = halfCylinderHeight;
            float r2 = r * r;

            float vCyl   = math.PI * r2 * (2f * h);
            float vSph   = (4f / 3f) * math.PI * r2 * r;
            float vTotal = vCyl + vSph;

            float mCyl = mass * vCyl / vTotal;
            float mSph = mass * vSph / vTotal;

            axialInertia = 0.5f * mCyl * r2
                         + 0.4f * mSph * r2;

            float cmOffset = h + 3f * r / 8f;
            lateralInertia = mCyl * (r2 / 4f + h * h / 3f)
                           + (83f / 160f) * mSph * r2
                           + mSph * cmOffset * cmOffset;
        }

        private static float GetAngularEffectScalar(
            in PhysicsBodyData body,
            float3 radiusVec,
            float3 normal)
        {
            float mass     = body.RigidbodyData.Mass;
            float3 crossVec = math.cross(radiusVec, normal);

            switch (body.ShapeType)
            {
                case ShapeType.Capsule:
                {
                    float radius     = body.PositionData.Scale * 0.5f;
                    float halfHeight = math.length(body.PositionData.Up) * 0.5f;
                    GetCapsuleInertia(mass, radius, halfHeight, out float axialInertia, out float lateralInertia);
                    float3 capsuleAxis  = math.normalizesafe(body.PositionData.Up);
                    float axialComp    = math.dot(crossVec, capsuleAxis);
                    float lateralLenSq = math.lengthsq(crossVec) - axialComp * axialComp;
                    return axialComp * axialComp / axialInertia + lateralLenSq / lateralInertia;
                }
                case ShapeType.SimplePlane:
                    return 0f;
                default:
                {
                    float radius  = body.PositionData.Scale * 0.5f;
                    float inertia = 0.4f * mass * radius * radius;
                    return math.dot(crossVec, crossVec) / inertia;
                }
            }
        }

        private static float3 ApplyInverseTensor(in PhysicsBodyData body, float3 torque)
        {
            float mass = body.RigidbodyData.Mass;

            switch (body.ShapeType)
            {
                case ShapeType.Capsule:
                {
                    float radius     = body.PositionData.Scale * 0.5f;
                    float halfHeight = math.length(body.PositionData.Up) * 0.5f;
                    GetCapsuleInertia(mass, radius, halfHeight, out float axialInertia, out float lateralInertia);
                    float3 capsuleAxis   = math.normalizesafe(body.PositionData.Up);
                    float3 axialTorque   = math.dot(torque, capsuleAxis) * capsuleAxis;
                    float3 lateralTorque = torque - axialTorque;
                    return axialTorque / axialInertia + lateralTorque / lateralInertia;
                }
                case ShapeType.SimplePlane:
                    return float3.zero;
                default:
                {
                    float radius  = body.PositionData.Scale * 0.5f;
                    float inertia = 0.4f * mass * radius * radius;
                    return torque / inertia;
                }
            }
        }

        public static float GetLowestPointY(in PhysicsBodyData body)
        {
            switch (body.ShapeType)
            {
                case ShapeType.Capsule:
                {
                    float radius = body.PositionData.Scale * 0.5f;
                    float3 capA = body.PositionData.Position - body.PositionData.Up * 0.5f;
                    float3 capB = body.PositionData.Position + body.PositionData.Up * 0.5f;
                    float lowestCapY = math.min(capA.y, capB.y);
                    return lowestCapY - radius;
                }
                case ShapeType.SimplePlane:
                    return body.PositionData.Position.y;
                default:
                    return body.PositionData.Position.y - body.PositionData.Scale * 0.5f;
            }
        }

        public static float GetHighestPointY(in PhysicsBodyData body)
        {
            switch (body.ShapeType)
            {
                case ShapeType.Capsule:
                {
                    float radius = body.PositionData.Scale * 0.5f;
                    float3 capA = body.PositionData.Position - body.PositionData.Up * 0.5f;
                    float3 capB = body.PositionData.Position + body.PositionData.Up * 0.5f;
                    float highestCapY = math.max(capA.y, capB.y);
                    return highestCapY + radius;
                }
                case ShapeType.SimplePlane:
                    return body.PositionData.Position.y;
                default:
                    return body.PositionData.Position.y + body.PositionData.Scale * 0.5f;
            }
        }

        private static float GetPenetrationDepth(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            float3 contactPoint)
        {
            if (body1.ShapeType == ShapeType.SimplePlane)
            {
                return body1.PositionData.Position.y - GetLowestPointY(body2);
            }
            if (body2.ShapeType == ShapeType.SimplePlane)
            {
                return body2.PositionData.Position.y - GetLowestPointY(body1);
            }

            float radius1 = body1.PositionData.Scale * 0.5f;
            float radius2 = body2.PositionData.Scale * 0.5f;
            float3 delta = GetDelta(body1, body2, contactPoint);
            float distance = math.length(delta);
            return (radius1 + radius2) - distance;
        }

        private static float3 closestPointOnSegment(float3 a, float3 b, float3 point)
        {
            float3 ab = b - a;
            float abLenSq = math.lengthsq(ab);
            if (abLenSq < 0.0001f)
            {
                return a;
            }
            float t = math.clamp(math.dot(point - a, ab) / abLenSq, 0f, 1f);
            return a + ab * t;
        }

        /// <summary>
        /// Calculates collision impulses for a pair of bodies at the given contact point.
        /// Static bodies and triggers receive zero impulse.
        /// Dynamic-vs-dynamic accounts for angular velocity and moment of inertia.
        /// Dynamic-vs-static treats the static body as immovable.
        /// </summary>
        public static void GetCollisionImpulses(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            in VelocityData vel1,
            in VelocityData vel2,
            float3 contactPoint,
            out float3 impulse1,
            out float3 impulse2)
        {
            impulse1 = float3.zero;
            impulse2 = float3.zero;

            bool body1Dynamic = body1.IsDynamic;
            bool body2Dynamic = body2.IsDynamic;

            if (!body1Dynamic && !body2Dynamic)
            {
                return;
            }

            float3 delta = GetDelta(body1, body2, contactPoint);
            float distSq = math.lengthsq(delta);
            if (distSq < 0.0001f)
            {
                return;
            }

            float distance = math.sqrt(distSq);
            float3 normal = delta / distance;

            if (body1Dynamic && body2Dynamic)
            {
                calculateDynamicVsDynamic(body1, body2, vel1, vel2, normal, contactPoint,
                    out impulse1, out impulse2);
            }
            else if (body1Dynamic)
            {
                calculateDynamicVsStatic(body1, body2, vel1, normal, contactPoint, out impulse1, out impulse2);
            }
            else
            {
                calculateDynamicVsStatic(body2, body1, vel2, -normal, contactPoint, out impulse2, out impulse1);
            }
        }

        /// <summary>
        /// Calculates the collision impulse for body1 only (Jacobi: body1 computes its own side).
        /// Skips if body1 is not dynamic or if bodies are separating.
        /// </summary>
        public static void GetCollisionImpulse(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            in VelocityData vel1,
            in VelocityData vel2,
            float3 contactPoint,
            out float3 impulse)
        {
            impulse = float3.zero;

            if (!body1.IsDynamic)
            {
                return;
            }

            float3 delta = GetDelta(body1, body2, contactPoint);
            float distSq = math.lengthsq(delta);
            if (distSq < 0.0001f)
            {
                return;
            }

            float3 normal = delta * math.rsqrt(distSq);
            float avgBounciness = (body1.RigidbodyData.Bounciness + body2.RigidbodyData.Bounciness) * 0.5f;

            if (body2.IsDynamic)
            {
                float3 radiusI = GetDynamicsRadius(body1, contactPoint);
                float3 radiusJ = GetDynamicsRadius(body2, contactPoint);

                float3 velAtContactI = vel1.Linear + math.cross(vel1.Angular, radiusI);
                float3 velAtContactJ = vel2.Linear + math.cross(vel2.Angular, radiusJ);
                float relVelAlongNormal = math.dot(velAtContactJ - velAtContactI, normal);

                if (relVelAlongNormal >= 0f)
                {
                    return;
                }

                float angularEffect = GetAngularEffectScalar(body1, radiusI, normal)
                                    + GetAngularEffectScalar(body2, radiusJ, normal);

                float impulseMag = -(1f + avgBounciness) * relVelAlongNormal
                                   / (1f / body1.RigidbodyData.Mass + 1f / body2.RigidbodyData.Mass + angularEffect);

                impulse = -normal * impulseMag;
                return;
            }
            else
            {
                float3 radiusI           = GetDynamicsRadius(body1, contactPoint);
                float3 velAtContactI     = vel1.Linear + math.cross(vel1.Angular, radiusI);
                float  relVelAlongNormal = math.dot(-velAtContactI, normal);

                if (relVelAlongNormal >= 0f)
                {
                    return;
                }

                float angularEffect = GetAngularEffectScalar(body1, radiusI, normal);
                float impulseMag = -(1f + avgBounciness) * relVelAlongNormal
                                   / (1f / body1.RigidbodyData.Mass + angularEffect);
                impulse = -normal * impulseMag;
            }
        }

        /// <summary>
        /// Calculates push-out separation forces for a pair of penetrating bodies.
        /// Static and trigger bodies are not pushed. At least one body must be dynamic.
        /// Force magnitude is proportional to penetration depth and Hardness.
        /// </summary>
        public static void GetPushOutForce(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            float3 contactPoint,
            out float3 force1,
            out float3 force2)
        {
            force1 = float3.zero;
            force2 = float3.zero;

            if (body1.IsTrigger)
            {
                return;
            }
            if (body2.IsTrigger)
            {
                return;
            }

            bool body1Dynamic = body1.IsDynamic;
            bool body2Dynamic = body2.IsDynamic;

            if (!body1Dynamic && !body2Dynamic)
            {
                return;
            }

            if (body1.RigidbodyData.Hardness == 0f && body2.RigidbodyData.Hardness == 0f)
            {
                return;
            }

            float penetration = GetPenetrationDepth(body1, body2, contactPoint);

            if (penetration <= 0f)
            {
                return;
            }

            float3 delta = GetDelta(body1, body2, contactPoint);
            float distance = math.length(delta);
            float3 normal = delta / math.max(distance, 0.0001f);

            const float MinPower = 0.02f;
            const float MaxPower = 0.5f;

            if (body1Dynamic)
            {
                force1 = -normal * penetration * math.lerp(MinPower, MaxPower, body2.RigidbodyData.Hardness);
            }

            if (body2Dynamic)
            {
                force2 = normal * penetration * math.lerp(MinPower, MaxPower, body1.RigidbodyData.Hardness);
            }
        }

        /// <summary>
        /// Calculates the push-out separation force for body1 only.
        /// Skips if body1 is not dynamic, either body is a trigger, or hardness is zero.
        /// </summary>
        public static void GetPushOutForce(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            float3 contactPoint,
            out float3 force1)
        {
            force1 = float3.zero;

            if (!body1.IsDynamic || body1.IsTrigger || body2.IsTrigger)
            {
                return;
            }

            if (body1.RigidbodyData.Hardness == 0f && body2.RigidbodyData.Hardness == 0f)
            {
                return;
            }

            float penetration = GetPenetrationDepth(body1, body2, contactPoint);

            if (penetration <= 0f)
            {
                return;
            }

            float3 delta = GetDelta(body1, body2, contactPoint);
            float distance = math.length(delta);
            float3 normal = delta / math.max(distance, 0.0001f);

            const float MinPower = 0.02f;
            const float MaxPower = 0.5f;

            force1 = -normal * penetration * math.lerp(MinPower, MaxPower, body2.RigidbodyData.Hardness);
        }

        /// <summary>
        /// Converts an impulse applied at a contact point into linear and angular velocity changes.
        /// Uses per-shape inertia: isotropic sphere tensor for Sphere/ReverseSphere,
        /// non-isotropic capsule tensor (axial vs lateral) for Capsule, zero for SimplePlane.
        /// </summary>
        public static void ImpulseToVelocity(
            in PhysicsBodyData body,
            float3 impulse,
            float3 contactPoint,
            out float3 linearVelocityChange,
            out float3 angularVelocityChange)
        {
            float mass = body.RigidbodyData.Mass;
            float3 dynamicsRadius = GetDynamicsRadius(body, contactPoint);
            float3 torque = math.cross(dynamicsRadius, impulse);
            linearVelocityChange = impulse / mass;
            angularVelocityChange = ApplyInverseTensor(body, torque);
        }

        private static void calculateDynamicVsDynamic(
            in PhysicsBodyData body1,
            in PhysicsBodyData body2,
            in VelocityData vel1,
            in VelocityData vel2,
            float3 normal,
            float3 contactPoint,
            out float3 impulse1,
            out float3 impulse2)
        {
            impulse1 = float3.zero;
            impulse2 = float3.zero;

            float3 radiusI = GetDynamicsRadius(body1, contactPoint);
            float3 radiusJ = GetDynamicsRadius(body2, contactPoint);

            float3 velAtContactI = vel1.Linear + math.cross(vel1.Angular, radiusI);
            float3 velAtContactJ = vel2.Linear + math.cross(vel2.Angular, radiusJ);
            float relVelAlongNormal = math.dot(velAtContactJ - velAtContactI, normal);

            if (relVelAlongNormal >= 0f)
            {
                return;
            }

            float avgBounciness = (body1.RigidbodyData.Bounciness + body2.RigidbodyData.Bounciness) * 0.5f;

            float angularEffect = GetAngularEffectScalar(body1, radiusI, normal)
                                + GetAngularEffectScalar(body2, radiusJ, normal);

            float impulseMag = -(1.0f + avgBounciness) * relVelAlongNormal
                               / (1.0f / body1.RigidbodyData.Mass + 1.0f / body2.RigidbodyData.Mass + angularEffect);

            float3 impulse = normal * impulseMag;
            impulse1 = -impulse;
            impulse2 = impulse;
        }

        private static void calculateDynamicVsStatic(
            in PhysicsBodyData dynBody,
            in PhysicsBodyData staticBody,
            in VelocityData dynVel,
            float3 normal,
            float3 contactPoint,
            out float3 dynImpulse,
            out float3 staticImpulse)
        {
            dynImpulse    = float3.zero;
            staticImpulse = float3.zero;

            float3 radiusVec         = GetDynamicsRadius(dynBody, contactPoint);
            float3 velAtContact      = dynVel.Linear + math.cross(dynVel.Angular, radiusVec);
            float  relVelAlongNormal = math.dot(-velAtContact, normal);

            if (relVelAlongNormal >= 0f)
            {
                return;
            }

            float avgBounciness = (dynBody.RigidbodyData.Bounciness + staticBody.RigidbodyData.Bounciness) * 0.5f;
            float angularEffect = GetAngularEffectScalar(dynBody, radiusVec, normal);
            float impulseMag = -(1.0f + avgBounciness) * relVelAlongNormal
                               / (1.0f / dynBody.RigidbodyData.Mass + angularEffect);
            float3 impulse = normal * impulseMag;

            dynImpulse    = -impulse;
            staticImpulse =  impulse;
        }
    }
}
