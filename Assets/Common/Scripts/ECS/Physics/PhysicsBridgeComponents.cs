using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace LittlePhysics
{
    public struct BodiesPair : System.IEquatable<BodiesPair>
    {
        public Entity Entity1;
        public Entity Entity2;

        public bool Equals(BodiesPair other)
        {
            return Entity1.Equals(other.Entity1) && Entity2.Equals(other.Entity2);
        }

        public override int GetHashCode()
        {
            return unchecked((int)(Entity1.Index ^ Entity2.Index));
        }
    }

    public struct SurfaceCollisionData
    {
        public bool IsColliding;
        public float3 ContactPoint;
    }

    public struct CollisionMapSingleton
    {
        [NoAlias] public ListsArray<uint> DynamicMap;
        [NoAlias] public ListsArray<uint> StaticMap;
        [NoAlias] public NativeArray<SurfaceCollisionData> SurfaceCollisionMap;
    }

    public struct CollisionItem
    {
        public Entity Target;
        public float3 ContactPoint;
    }

    /// <summary>
    /// Per-body collision record written by ImpulseDetectionSystem.
    /// Each body receives its own entry with impulse and push-out computed specifically
    /// for that body's side of the pair.
    /// </summary>
    public struct CollisionData
    {
        public Entity Other;
        public uint OtherIndex;
        public float3 ContactPoint;
        public float3 Impulse;
        public float3 PushOut;
    }

    public struct CollisionsSingleton
    {
        [NoAlias] public ListsArray<CollisionData> CollisionDataMap;
    }

    // Kept for backward compatibility with PhysicsBodyComponent authoring
    public enum ColliderType : byte
    {
        Sphere,
        Capsule,
        SimplePlane,
        ReverseSphere
    }

    public struct VelocityData
    {
        public bool IsRotationBlocked;
        public float3 Linear;
        public float3 Angular;
    }

    public struct RigidbodyData
    {
        public float Mass;
        public float Bounciness;
        public float Friction;
        public float Hardness;
    }

    public struct MapUpdateData
    {
        public bool ShouldUpdate;
        public AABB CachedAABB;
    }

    public struct PhysicsBodyData
    {
        public Entity Main;
        public int Layer;
        public int LodIndex;

        public bool IsTrigger;
        public bool IsStatic;

        public ShapeType ShapeType;

        public PositionData PositionData;
        public VelocityData VelocityData;
        public RigidbodyData RigidbodyData;
        public MapUpdateData MapUpdateData;

        public bool IsDynamic => !IsStatic && !IsTrigger;

        public Sphere GetSphere() => new Sphere { Position = PositionData.Position, Scale = PositionData.Scale };
        public Capsule GetCapsule() => new Capsule { Position = PositionData.Position, Up = PositionData.Up, Scale = PositionData.Scale };
        public SimplePlane GetSimplePlane() => new SimplePlane { Y = PositionData.Position.y };
        public InverseSphere GetInverseSphere() => new InverseSphere { Position = PositionData.Position, Scale = PositionData.Scale };
    }

    public struct PhysicsSingleton : IComponentData
    {
        [NoAlias] public NativeArray<PhysicsBodyData> BodiesList;
        [NoAlias] public NativeArray<Random> Randoms;
        public CollisionMapSingleton CollisionMap;
        public CollisionsSingleton Collisions;
        public SpacialMap SpacialMap;
        public PhysicsSettingsComponent Settings;
        public JobHandle PhysicsJobHandle;
    }
}
