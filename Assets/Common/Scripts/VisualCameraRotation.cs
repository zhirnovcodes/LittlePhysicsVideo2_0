using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class VisualCameraRotation : VisualBase
{
    [SerializeField] private Transform CameraRotationHandle;
    [SerializeField] private float RotationSpeed = 5f;   // degrees/sec
    [SerializeField] private bool ShouldWaitInput;

    public override void Init() { }

    public override async UniTask Run(IVisualController controller)
    {
        if (ShouldWaitInput)
        {
            bool next = await controller.WaitNextClicked();
            if (!next)
            {
                return;
            }
        }

        // Derive duration so the handle completes exactly one full 360-degree turn
        float rotationDuration = 360f / Mathf.Abs( RotationSpeed );

        // Rotate the camera rotation handle 360 degrees around Y
        await CameraRotationHandle.DOLocalRotate(new Vector3(0f, Mathf.Sign(RotationSpeed) * 360f, 0f), rotationDuration, RotateMode.FastBeyond360).ToUniTask();
    }
}
