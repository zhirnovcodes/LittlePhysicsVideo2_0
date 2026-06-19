using UnityEngine;

public class RotateAroundAxis : MonoBehaviour
{
    public Vector3 Axis = Vector3.up;
    public float SpeedDegreePerSec = 90f;

    void Update()
    {
        transform.Rotate(Axis.normalized, SpeedDegreePerSec * Time.deltaTime, Space.Self);
    }
}
