using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Visual_11_2 : VisualBase
{
    [SerializeField] private Transform Cell;
    [SerializeField] private Axiz Axis;
    [SerializeField] private Transform CameraRotationHandle;

    private const float RotationDegreePerSecond = 7f;
    private const float AppearTime = 2f;

    public override void Init()
    {
        // Collapse the axis to its zero state
        Axis.SetT(0f);

        // Set the cell to nearly zero scale so it is invisible at start
        Cell.localScale = Vector3.one * 0.001f;
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Fire-and-forget continuous Y-axis camera orbit at RotationDegreePerSecond degrees/sec
        float rotationDuration = 360f / Mathf.Abs(RotationDegreePerSecond);
        Vector3 startEuler = CameraRotationHandle.localEulerAngles;
        CameraRotationHandle.DOLocalRotate(
            new Vector3(startEuler.x, startEuler.y + Mathf.Sign(RotationDegreePerSecond) * 360f, startEuler.z),
            rotationDuration,
            RotateMode.FastBeyond360
        ).SetLoops(-1);

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Tween the axis from 0 to 1 and scale the cell to full size simultaneously over AppearTime
        await DOTween.Sequence()
            .Append(DOVirtual.Float(0f, 1f, AppearTime, value =>
            {
                Axis.SetT(value);
            }))
            .Join(Cell.DOScale(Vector3.one, AppearTime))
            .ToUniTask();
    }
}
