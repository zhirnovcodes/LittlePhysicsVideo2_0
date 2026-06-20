using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_5_1 : VisualBase
{
    [SerializeField] private TMP_Text CountText;
    [SerializeField] private TMP_Text CapacityText;
    [SerializeField] private Image Background;
    [SerializeField] private TMP_Text ListArrayText;
    [SerializeField] private TMP_Text CountArrayText;
    [SerializeField] private List<RectTransform> ArrayCellsImages;
    [SerializeField] private List<RectTransform> ArrayCellsPlaceholders;
    [SerializeField] private RectTransform CellsAnimationPanel;

    private const float AppearTime = 0.8f;
    private const float MoveTime = 0.08f;
    private const float MoveInterval = 0.02f;

    private List<Vector3> CellsInitialPositions;

    public override void Init()
    {
        // Hide Count and Capacity texts
        CountText.alpha = 0f;
        CapacityText.alpha = 0f;

        // Hide ListArray and CountArray texts
        ListArrayText.alpha = 0f;
        CountArrayText.alpha = 0f;

        // Hide the background image
        Background.color = new Color(Background.color.r, Background.color.g, Background.color.b, 0f);

        // Force all layout groups to resolve so that .position reflects actual screen positions
        Canvas.ForceUpdateCanvases();

        // Reparent each cell image to the animation panel, manually restoring world position
        foreach (RectTransform cell in ArrayCellsImages)
        {
            Vector3 worldPos = cell.position;
            cell.SetParent(CellsAnimationPanel, false);
            cell.position = worldPos;
        }

        // Reparent each placeholder to the animation panel, manually restoring world position
        foreach (RectTransform placeholder in ArrayCellsPlaceholders)
        {
            Vector3 worldPos = placeholder.position;
            placeholder.SetParent(CellsAnimationPanel, false);
            placeholder.position = worldPos;
        }

        // Save the world position of each cell image as its starting position
        CellsInitialPositions = new List<Vector3>();
        foreach (RectTransform cell in ArrayCellsImages)
        {
            CellsInitialPositions.Add(cell.position);
        }
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade Count text in, then Capacity text in one after the other
        await CountText.DOFade(1f, AppearTime).ToUniTask();
        await CapacityText.DOFade(1f, AppearTime).ToUniTask();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade background, ListArray and CountArray in simultaneously
        await DOTween.Sequence()
            .Join(Background.DOFade(1f, AppearTime))
            .Join(ListArrayText.DOFade(1f, AppearTime))
            .Join(CountArrayText.DOFade(1f, AppearTime))
            .ToUniTask();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep3MoveCellsToPlaceholders();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep4MoveCellsBack();
    }

    private async UniTask RunStep3MoveCellsToPlaceholders()
    {
        // Build a staggered sequence: each cell starts moving MoveInterval after the previous one
        var sequence = DOTween.Sequence();
        for (int i = 0; i < ArrayCellsImages.Count; i++)
        {
            sequence.Insert(i * MoveInterval, ArrayCellsImages[i].DOMove(ArrayCellsPlaceholders[i].position, MoveTime));
        }
        // Wait for the full staggered animation to finish
        await sequence.ToUniTask();
    }

    private async UniTask RunStep4MoveCellsBack()
    {
        // Build a staggered sequence moving each cell back to its saved initial position
        var sequence = DOTween.Sequence();
        for (int i = 0; i < ArrayCellsImages.Count; i++)
        {
            sequence.Insert(i * MoveInterval, ArrayCellsImages[i].DOMove(CellsInitialPositions[i], MoveTime));
        }
        // Wait for the full staggered animation to finish
        await sequence.ToUniTask();
    }
}
