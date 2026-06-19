using UnityEngine;

public class BrownMovement : MonoBehaviour
{
    [SerializeField] private float MaxRadius = 2f;
    [SerializeField] private float Speed = 1f;

    private Vector3 StartPosition;
    private Vector3 TargetPosition;

    private void Start()
    {
        StartPosition = transform.position;
        TargetPosition = PickRandomTarget();
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, TargetPosition, Speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, TargetPosition) < 0.01f)
        {
            TargetPosition = PickRandomTarget();
        }
    }

    private Vector3 PickRandomTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * MaxRadius;
        return StartPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }
}
