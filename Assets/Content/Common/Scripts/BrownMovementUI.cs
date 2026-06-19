using UnityEngine;

public class BrownMovementUI : MonoBehaviour
{
    [SerializeField] private float MaxRadius = 100f;
    [SerializeField] private float Speed = 100f;

    private RectTransform RectTransform;
    private Vector2 StartPosition;
    private Vector2 TargetPosition;

    private void Start()
    {
        RectTransform = GetComponent<RectTransform>();
        StartPosition = RectTransform.anchoredPosition;
        TargetPosition = PickRandomTarget();
    }

    private void Update()
    {
        RectTransform.anchoredPosition = Vector2.MoveTowards(RectTransform.anchoredPosition, TargetPosition, Speed * Time.deltaTime);

        if (Vector2.Distance(RectTransform.anchoredPosition, TargetPosition) < 0.5f)
        {
            TargetPosition = PickRandomTarget();
        }
    }

    private Vector2 PickRandomTarget()
    {
        return StartPosition + Random.insideUnitCircle * MaxRadius;
    }
}
