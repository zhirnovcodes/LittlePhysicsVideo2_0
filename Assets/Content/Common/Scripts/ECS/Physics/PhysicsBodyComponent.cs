using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace LittlePhysics
{
    public enum BodyType
    {
        Dynamic,
        Static,
        Trigger
    }

    public enum UpdateType
    {
        EveryFrame,
        Once,
        WithInterval
    }

    public struct PhysicsBodyUpdateComponent : IComponentData, IEnableableComponent
    {
        public UpdateType Type;
        public float Interval;
        public bool WasUpdated;
        public float TimeElapsed;
        public int Index;
        public int LodIndex;
        public bool IsEnabled;
    }

    public struct PhysicsBodyComponent : IComponentData
    {
        public BodyType BodyType;
        public ColliderType ColliderType;
        public float3 LocalPosition;
        public float3 RotationOffset;
        public float Scale;
        public float Mass;
        public float Bounciness;
        public float Friction;
        public float Hardness;
        public Entity Main;
        public int Layer;
        public bool ShouldRotateOnCollision;

        public PhysicsBodyData ToBodyData(LocalToWorld localToWorld, int lodIndex, bool shouldUpdateMap = true)
        {
            return new PhysicsBodyData
            {
                Main = Main,
                IsStatic = BodyType == BodyType.Static,
                IsTrigger = BodyType == BodyType.Trigger,
                ShapeType = (ShapeType)ColliderType,
                PositionData = new PositionData
                {
                    Position = localToWorld.Position + LocalPosition,
                    RotationOffset = RotationOffset,
                    Scale = math.length(localToWorld.Value.c0.xyz) * Scale,
                    Up = math.rotate(localToWorld.Rotation, math.up()),
                },
                LodIndex = lodIndex,
                Layer = Layer,
                RigidbodyData = new RigidbodyData
                {
                    Mass = Mass,
                    Bounciness = Bounciness,
                    Friction = Friction,
                    Hardness = Hardness,
                },
                MapUpdateData = new MapUpdateData
                {
                    ShouldUpdate = shouldUpdateMap,
                },
                VelocityData = new VelocityData
                {
                    IsRotationBlocked = !ShouldRotateOnCollision,
                },
            };
        }
    }
}
