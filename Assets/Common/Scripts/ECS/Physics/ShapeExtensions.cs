using Unity.Mathematics;

namespace LittlePhysics
{
    public static class ShapeExtensions
    {
        public static bool IsIntersect(
            ShapeType shape1, PositionData pos1,
            ShapeType shape2, PositionData pos2,
            out float3 contactPoint)
        {
            switch (shape1, shape2)
            {
                case (ShapeType.Sphere, ShapeType.Sphere):
                    return CollisionMethods.AreSpheresColliding(
                        new Sphere { Position = pos1.Position, Scale = pos1.Scale },
                        new Sphere { Position = pos2.Position, Scale = pos2.Scale },
                        out contactPoint);

                case (ShapeType.Sphere, ShapeType.Capsule):
                    return CollisionMethods.IsSphereCollidingCapsule(
                        new Sphere { Position = pos1.Position, Scale = pos1.Scale },
                        new Capsule { Position = pos2.Position, Up = pos2.Up, Scale = pos2.Scale },
                        out contactPoint);

                case (ShapeType.Capsule, ShapeType.Sphere):
                    return CollisionMethods.IsSphereCollidingCapsule(
                        new Sphere { Position = pos2.Position, Scale = pos2.Scale },
                        new Capsule { Position = pos1.Position, Up = pos1.Up, Scale = pos1.Scale },
                        out contactPoint);

                case (ShapeType.Sphere, ShapeType.SimplePlane):
                    return CollisionMethods.IsSphereCollidingSimplePlane(
                        new Sphere { Position = pos1.Position, Scale = pos1.Scale },
                        new SimplePlane { Y = pos2.Position.y },
                        out contactPoint);

                case (ShapeType.Capsule, ShapeType.SimplePlane):
                    return CollisionMethods.IsCapsuleCollidingSimplePlane(
                        new Capsule { Position = pos1.Position, Up = pos1.Up, Scale = pos1.Scale },
                        new SimplePlane { Y = pos2.Position.y },
                        out contactPoint);

                case (ShapeType.SimplePlane, ShapeType.Sphere):
                    return CollisionMethods.IsSphereCollidingSimplePlane(
                        new Sphere { Position = pos2.Position, Scale = pos2.Scale },
                        new SimplePlane { Y = pos1.Position.y },
                        out contactPoint);

                case (ShapeType.SimplePlane, ShapeType.Capsule):
                    return CollisionMethods.IsCapsuleCollidingSimplePlane(
                        new Capsule { Position = pos2.Position, Up = pos2.Up, Scale = pos2.Scale },
                        new SimplePlane { Y = pos1.Position.y },
                        out contactPoint);

                case (ShapeType.Sphere, ShapeType.ReverseSphere):
                    return CollisionMethods.IsSphereCollidingReverseSphere(
                        new Sphere { Position = pos1.Position, Scale = pos1.Scale },
                        new InverseSphere { Position = pos2.Position, Scale = pos2.Scale },
                        out contactPoint);

                case (ShapeType.ReverseSphere, ShapeType.Sphere):
                    return CollisionMethods.IsSphereCollidingReverseSphere(
                        new Sphere { Position = pos2.Position, Scale = pos2.Scale },
                        new InverseSphere { Position = pos1.Position, Scale = pos1.Scale },
                        out contactPoint);

                default:
                    contactPoint = float3.zero;
                    return false;
            }
        }

        public static Rectangle GetRectangle(ShapeType shapeType, PositionData pos)
        {
            switch (shapeType)
            {
                case ShapeType.Capsule:
                {
                    float radius = pos.Scale * 0.5f;
                    float3 halfAxis = pos.Up * 0.5f;
                    float3 halfExtents = math.abs(halfAxis) + new float3(radius, radius, radius);
                    return new Rectangle
                    {
                        Position = pos.Position,
                        Scale = halfExtents * 2f
                    };
                }
                case ShapeType.SimplePlane:
                {
                    return new Rectangle 
                    {
                        Position = pos.Position,
                        Scale = new float3(1000000f, 0.00001f, 1000000f)
                    };
                }
                default:
                    return new Rectangle
                    {
                        Position = pos.Position,
                        Scale = new float3(pos.Scale, pos.Scale, pos.Scale)
                    };
            }
        }
    }
}
