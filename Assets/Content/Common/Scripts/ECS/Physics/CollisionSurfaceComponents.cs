using Unity.Entities;
using Unity.Mathematics;

namespace LittlePhysics
{
    public enum SurfaceType
    {
        SimplePlane,
        Sphere,
        ReverseSphere
    }

    public struct CollisionSurfaceComponent : IComponentData
    {
        public SurfaceType SurfaceType;
        public SimplePlane Plane;
        public Sphere Sphere;
        public float Bounciness;
        public float Hardness;
        public float Friction;
        public int Layer;

        public PhysicsBodyData ToBodyData()
        {
            switch (SurfaceType)
            {
                case SurfaceType.SimplePlane:
                {
                    return new PhysicsBodyData
                    {
                        IsStatic = true,
                        ShapeType = ShapeType.SimplePlane,
                        PositionData = new PositionData { Position = new float3(0f, Plane.Y, 0f) },
                        Layer = Layer,
                        RigidbodyData = new RigidbodyData
                        {
                            Bounciness = Bounciness,
                            Hardness = Hardness,
                            Friction = Friction,
                        },
                    };
                }
                case SurfaceType.Sphere:
                {
                    return new PhysicsBodyData
                    {
                        IsStatic = true,
                        ShapeType = ShapeType.Sphere,
                        PositionData = new PositionData { Position = Sphere.Position, Scale = Sphere.Scale },
                        Layer = Layer,
                        RigidbodyData = new RigidbodyData
                        {
                            Bounciness = Bounciness,
                            Hardness = Hardness,
                            Friction = Friction,
                        },
                    };
                }
                case SurfaceType.ReverseSphere:
                {
                    return new PhysicsBodyData
                    {
                        IsStatic = true,
                        ShapeType = ShapeType.ReverseSphere,
                        PositionData = new PositionData { Position = Sphere.Position, Scale = Sphere.Scale },
                        Layer = Layer,
                        RigidbodyData = new RigidbodyData
                        {
                            Bounciness = Bounciness,
                            Hardness = Hardness,
                            Friction = Friction,
                        },
                    };
                }
            }

            return new PhysicsBodyData
            {
                IsStatic = true,
                ShapeType = ShapeType.Sphere,
                PositionData = new PositionData { Position = Sphere.Position, Scale = Sphere.Scale },
                Layer = Layer,
                RigidbodyData = new RigidbodyData
                {
                    Bounciness = Bounciness,
                    Hardness = Hardness,
                    Friction = Friction,
                },
            };
        }
    }
}
