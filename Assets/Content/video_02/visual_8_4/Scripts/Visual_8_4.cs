using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_8_4 : VisualBase
{
    [Header("Captions")]
    [SerializeField] private TMP_Text Caption0;
    [SerializeField] private TMP_Text Caption1;
    [SerializeField] private TMP_Text Caption2;
    [SerializeField] private TMP_Text Caption3;

    [Header("Left Side")]
    [SerializeField] private List<Image> CellsLeft;
    [SerializeField] private List<Image> EntitiesInCellsLeft1;
    [SerializeField] private List<Image> EntitiesInCellsLeft2;
    [SerializeField] private List<Image> PairsLeft;
    [SerializeField] private Image Cross0;
    [SerializeField] private Image Cross1;
    [SerializeField] private Image Cross2;

    [Header("Right Side")]
    [SerializeField] private List<Image> CellsRight;
    [SerializeField] private Image SharedCellsRight;
    [SerializeField] private List<Image> CirclesInCellRight;
    [SerializeField] private List<Image> PairsRight;

    private const float MoveTime = 0.8f;
    private const float AppearTime = 0.6f;
    private const float MaxCellAlpha = 0.4f;
    private const float CellAppearInterval = 0.05f;

    public override void Init()
    {
        Canvas.ForceUpdateCanvases();

        // Hide all captions
        Caption0.alpha = 0f;
        Caption1.alpha = 0f;
        Caption2.alpha = 0f;
        Caption3.alpha = 0f;

        // Hide left-side image lists
        foreach (Image image in CellsLeft) { SetAlpha(image, 0f); }
        foreach (Image image in EntitiesInCellsLeft1) { SetAlpha(image, 0f); }
        foreach (Image image in EntitiesInCellsLeft2) { SetAlpha(image, 0f); }
        foreach (Image image in PairsLeft) { SetAlpha(image, 0f); }

        // Hide the three crosses
        SetAlpha(Cross0, 0f);
        SetAlpha(Cross1, 0f);
        SetAlpha(Cross2, 0f);

        // Hide right-side elements that start invisible
        foreach (Image image in CellsRight) { SetAlpha(image, 0f); }
        SetAlpha(SharedCellsRight, 0f);
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for user then reveal cells
        _ = await controller.WaitNextClicked();
        await RunStep1Cells();

        // Wait for user then reveal entities in cells
        _ = await controller.WaitNextClicked();
        await RunStep2EntitiesInCells();

        // Wait for user then reveal pairs
        _ = await controller.WaitNextClicked();
        await RunStep3Pairs();
    }

    private async UniTask RunStep1Cells()
    {
        // Fade Caption0 in while fading CellsRight in one by one with a staggered interval
        Sequence revealSequence = DOTween.Sequence();
        revealSequence.Join(Caption0.DOFade(1f, AppearTime));
        float delay = 0f;
        foreach (Image cell in CellsRight)
        {
            revealSequence.Insert(delay, cell.DOFade(MaxCellAlpha, AppearTime));
            delay += CellAppearInterval;
        }
        await revealSequence.ToUniTask();

        // Move all CellsRight to CellsLeft[0] position while fading them out
        Vector3 targetPosition = CellsLeft[0].transform.position;
        Sequence moveSequence = DOTween.Sequence();
        foreach (Image cell in CellsRight)
        {
            moveSequence.Join(cell.transform.DOMove(targetPosition, MoveTime));
            moveSequence.Join(cell.DOFade(0f, MoveTime));
        }
        await moveSequence.ToUniTask();

        // Fade all CellsLeft in to MaxCellAlpha
        Sequence cellsLeftSequence = DOTween.Sequence();
        foreach (Image cell in CellsLeft)
        {
            cellsLeftSequence.Join(cell.DOFade(MaxCellAlpha, AppearTime));
        }
        await cellsLeftSequence.ToUniTask();
    }

    private async UniTask RunStep2EntitiesInCells()
    {
        // Simultaneously fade in Cross0, Caption1, and SharedCellsRight
        await DOTween.Sequence()
            .Join(Cross0.DOFade(1f, AppearTime))
            .Join(Caption1.DOFade(1f, AppearTime))
            .Join(SharedCellsRight.DOFade(MaxCellAlpha, AppearTime))
            .ToUniTask();

        // Move all CirclesInCellRight to EntitiesInCellsLeft1[0] position while fading them out,
        // and simultaneously fade in all EntitiesInCellsLeft1
        Vector3 targetPosition = EntitiesInCellsLeft1[0].transform.position;
        Sequence transferSequence = DOTween.Sequence();
        foreach (Image circle in CirclesInCellRight)
        {
            transferSequence.Join(circle.transform.DOMove(targetPosition, MoveTime));
            transferSequence.Join(circle.DOFade(0f, MoveTime));
        }
        foreach (Image entity in EntitiesInCellsLeft1)
        {
            transferSequence.Join(entity.DOFade(1f, AppearTime));
        }
        await transferSequence.ToUniTask();

        // Simultaneously fade in Cross1, Caption2, and all EntitiesInCellsLeft2
        Sequence thenSequence = DOTween.Sequence();
        thenSequence.Join(Cross1.DOFade(1f, AppearTime));
        thenSequence.Join(Caption2.DOFade(1f, AppearTime));
        foreach (Image entity in EntitiesInCellsLeft2)
        {
            thenSequence.Join(entity.DOFade(1f, AppearTime));
        }
        await thenSequence.ToUniTask();

        // Fade SharedCellsRight out
        await SharedCellsRight.DOFade(0f, AppearTime).ToUniTask();
    }

    private async UniTask RunStep3Pairs()
    {
        // Simultaneously fade in Caption3, Cross2, and move all PairsRight to PairsLeft[0] while fading them out
        Vector3 targetPosition = PairsLeft[0].transform.position;
        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Join(Caption3.DOFade(1f, AppearTime));
        moveSequence.Join(Cross2.DOFade(1f, AppearTime));
        foreach (Image pair in PairsRight)
        {
            moveSequence.Join(pair.transform.DOMove(targetPosition, MoveTime));
            moveSequence.Join(pair.DOFade(0f, MoveTime));
        }
        await moveSequence.ToUniTask();

        // Fade all PairsLeft in
        Sequence pairsLeftSequence = DOTween.Sequence();
        foreach (Image pair in PairsLeft)
        {
            pairsLeftSequence.Join(pair.DOFade(1f, AppearTime));
        }
        await pairsLeftSequence.ToUniTask();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
