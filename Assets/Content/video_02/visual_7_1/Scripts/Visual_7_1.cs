using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_7_1 : VisualBase
{
    [SerializeField] private Axiz Axis;
    [SerializeField] private CameraHandler CurrentCameraHandle;
    [SerializeField] private CameraHandler TargetCameraHandle;
    [SerializeField] private List<TMP_Text> CellIndices;
    [SerializeField] private List<Image> Highlights;
    [SerializeField] private List<GameObject> Cells;
    [SerializeField] private List<MonoBehaviour> SpheresBehaviours;

    private const float RotateTime = 2f;
    private const float AppearTime = 0.8f;
    private const float BlinkTime = 0.3f;
    private const float CellIndexInterval = 0.05f;

    private Material SavedCellMaterial;

    public override void Init()
    {
        // Set all cell index labels to transparent
        foreach (TMP_Text label in CellIndices)
        {
            label.alpha = 0f;
        }

        // Set all highlight images to transparent
        foreach (Image highlight in Highlights)
        {
            SetAlpha(highlight, 0f);
        }

        // Save the shared material from the first cell, assign it to every cell, then hide it
        SavedCellMaterial = Cells[0].GetComponent<Renderer>().material;
        foreach (GameObject cell in Cells)
        {
            cell.GetComponent<Renderer>().material = SavedCellMaterial;
        }
        Color hiddenColor = SavedCellMaterial.color;
        hiddenColor.a = 0f;
        SavedCellMaterial.color = hiddenColor;

        // Collapse the axis to its zero state
        Axis.SetT(0f);
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep1AxisAndCamera();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep2HighlightsAppearAndBlink();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep3HideHighlightsAndLabels();
    }

    private async UniTask RunStep1AxisAndCamera()
    {
        foreach (MonoBehaviour behaviour in SpheresBehaviours)
        {
            behaviour.enabled = false;
        }

        // Snapshot the camera start and end states before animating
        CameraHandlerData startData = CurrentCameraHandle.GetData();
        CameraHandlerData targetData = TargetCameraHandle.GetData();

        // Grow the axis, fade in all cell index labels, fade in cell material, and lerp the camera — all in parallel
        var sequence = DOTween.Sequence()
            .Join(DOVirtual.Float(0f, 1f, RotateTime, t => Axis.SetT(t)))
            .Join(DOVirtual.Float(0f, 1f, RotateTime, t => CurrentCameraHandle.SetData(startData.Lerp(targetData, t))))
            .Join(SavedCellMaterial.DOFade(1f, RotateTime));

        // Stagger each cell index label to start appearing one by one after the main tweens finish
        for (int i = 0; i < CellIndices.Count; i++)
        {
            sequence.Insert(RotateTime + i * CellIndexInterval, CellIndices[i].DOFade(1f, AppearTime));
        }

        await sequence.ToUniTask();
    }

    private async UniTask RunStep2HighlightsAppearAndBlink()
    {
        const float maxAlpha = 0.6f;
        const float minAlpha = 0.4f;
        
        // Fade all highlights in simultaneously
        var sequence = DOTween.Sequence();
        foreach (Image highlight in Highlights)
        {
            sequence.Join(highlight.DOFade(maxAlpha, AppearTime));
        }
        await sequence.ToUniTask();

        // Start a continuous blink on each highlight: oscillate alpha between 1 and 0.9
        foreach (Image highlight in Highlights)
        {
            highlight.DOFade(minAlpha, BlinkTime).SetLoops(-1, LoopType.Yoyo);
        }
    }

    private async UniTask RunStep3HideHighlightsAndLabels()
    {
        // Stop the blink loop on every highlight
        foreach (Image highlight in Highlights)
        {
            highlight.DOKill();
        }

        // Fade highlights and cell index labels out simultaneously
        var sequence = DOTween.Sequence();
        foreach (Image highlight in Highlights)
        {
            sequence.Join(highlight.DOFade(0f, AppearTime));
        }
        foreach (TMP_Text label in CellIndices)
        {
            sequence.Join(label.DOFade(0f, AppearTime));
        }
        await sequence.ToUniTask();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
