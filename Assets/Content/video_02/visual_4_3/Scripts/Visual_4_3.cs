using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Visual_4_3 : VisualBase
{
    [SerializeField] private List<GameObject> Spheres;
    [SerializeField] private Transform Arrow;
    [SerializeField] private Transform CameraRotationHandle;

    [SerializeField] private float ArrowStartYPosition;
    [SerializeField] private float ArrowEndYPosition;
    [SerializeField] private float HighlightDeltaTime = 0.5f;
    [SerializeField] private float ArrowMoveTime = 2f;

    private const float RotationDegreePerSecond = 7f;
    private const float HighlightTime = 0.5f;

    private List<Material> SphereMaterials;

    public override void Init()
    {
        // Cache the renderer material of each sphere
        SphereMaterials = new List<Material>();
        foreach (GameObject sphere in Spheres)
        {
            SphereMaterials.Add(sphere.GetComponent<Renderer>().material);
        }

        // Place the arrow at the starting Y local position
        Vector3 arrowLocalPos = Arrow.localPosition;
        arrowLocalPos.y = ArrowStartYPosition;
        Arrow.localPosition = arrowLocalPos;
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Fire-and-forget continuous Y-axis rotation of the camera handle at RotationSpeed degrees/sec
        Vector3 startEuler = CameraRotationHandle.localEulerAngles;
        float rotationDuration = 360f / Mathf.Abs(RotationDegreePerSecond);
        CameraRotationHandle.DOLocalRotate(
            new Vector3(startEuler.x, startEuler.y + Mathf.Sign(RotationDegreePerSecond) * 360f, startEuler.z),
            rotationDuration,
            RotateMode.FastBeyond360
        ).SetLoops(-1);

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Start moving the arrow local Y from start to end position (fire-and-forget)
        Arrow.DOLocalMoveY(ArrowEndYPosition, ArrowMoveTime);

        // Snapshot the starting emission color for each sphere material
        List<Color> startEmissionColors = new List<Color>();
        foreach (Material mat in SphereMaterials)
        {
            startEmissionColors.Add(mat.GetColor("_EmissionColor"));
        }

        // Build a staggered sequence: after HighlightDeltaTime, fade each sphere's emission to black one by one
        Sequence highlightSequence = DOTween.Sequence();
        float delay = HighlightDeltaTime;
        for (int i = 0; i < SphereMaterials.Count; i++)
        {
            int index = i;
            highlightSequence.Insert(delay, DOVirtual.Float(1f, 0f, HighlightTime, value =>
            {
                SphereMaterials[index].SetColor("_EmissionColor", startEmissionColors[index] * value);
            }));
            delay += HighlightDeltaTime;
        }
        await highlightSequence.ToUniTask();
    }
}
