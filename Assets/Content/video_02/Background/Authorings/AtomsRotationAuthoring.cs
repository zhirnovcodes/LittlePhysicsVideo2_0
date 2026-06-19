using Unity.Entities;
using UnityEngine;

public sealed class AtomsRotationAuthoring : MonoBehaviour
{
    public float AttractionPower = 1f;
    public float Speed = 1f;

    private sealed class Baker : Baker<AtomsRotationAuthoring>
    {
        public override void Bake(AtomsRotationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AtomsRotationComponent
            {
                Speed = authoring.Speed,
                AttractionPower = authoring.AttractionPower
            });
        }
    }
}
