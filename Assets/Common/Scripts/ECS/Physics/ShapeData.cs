using Unity.Mathematics;

namespace LittlePhysics
{
    public enum ShapeType : byte
    {
        Sphere,
        Capsule,
        SimplePlane,
        ReverseSphere
    }

    public struct PositionData
    {
        public float3 Position;
        public float Scale;
        public float3 Up;
        public float3 RotationOffset;
    }

    public struct Rectangle
    {
        public float3 Position;
        public float3 Scale;
    }
}
