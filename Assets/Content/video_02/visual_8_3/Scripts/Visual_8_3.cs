using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_8_3 : VisualBase
{
    [Header("Left Side")]
    [SerializeField] private List<Image> NumEntitiesLeft;
    [SerializeField] private List<Image> CellsPerEntityLeft;
    [SerializeField] private List<Image> EntitiesInCellLeft;
    [SerializeField] private List<Image> MaxPairsLeft;
    [SerializeField] private Image Cross0;
    [SerializeField] private Image Cross1;
    [SerializeField] private Image Cross2;

    [Header("Captions")]
    [SerializeField] private TMP_Text CaptionStep1;
    [SerializeField] private TMP_Text CaptionStep2;
    [SerializeField] private TMP_Text CaptionStep3;
    [SerializeField] private TMP_Text CaptionStep4;

    [Header("Right Side")]
    [SerializeField] private List<Image> CirclesRight;
    [SerializeField] private List<Image> EntitiesInCellsRight;
    [SerializeField] private List<Image> PairsRight;
    [SerializeField] private List<Image> CellsPerEntityRight;
    [SerializeField] private Image CellSingle;

    private const float MoveTime = 0.8f;
    private const float AppearTime = 1f;
    private const float MaxCellAlpha = 0.4f;

    public override void Init()
    {        
        Canvas.ForceUpdateCanvases();

        // Hide all left-side image lists
        foreach (Image image in NumEntitiesLeft) { SetAlpha(image, 0f); }
        foreach (Image image in CellsPerEntityLeft) { SetAlpha(image, 0f); }
        foreach (Image image in EntitiesInCellLeft) { SetAlpha(image, 0f); }
        foreach (Image image in MaxPairsLeft) { SetAlpha(image, 0f); }

        // Hide the three crosses
        SetAlpha(Cross0, 0f);
        SetAlpha(Cross1, 0f);
        SetAlpha(Cross2, 0f);

        // Hide right-side elements that start invisible
        foreach (Image image in CellsPerEntityRight) { SetAlpha(image, 0f); }
        SetAlpha(CellSingle, 0f);

        // Hide all captions
        CaptionStep1.alpha = 0f;
        CaptionStep2.alpha = 0f;
        CaptionStep3.alpha = 0f;
        CaptionStep4.alpha = 0f;
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for user, then run step 1: circles → NumEntities
        _ = await controller.WaitNextClicked();
        await RunStep1NumEntities();

        // Wait for user, then run step 2: CellsPerEntity reveal and move
        _ = await controller.WaitNextClicked();
        await RunStep2CellsPerEntity();

        // Wait for user, then run step 3: CellSingle reveal, EntitiesInCell move
        _ = await controller.WaitNextClicked();
        await RunStep3EntitiesInCell();

        // Wait for user, then run step 4: Pairs move
        _ = await controller.WaitNextClicked();
        await RunStep4MaxPairs();
    }

    private async UniTask RunStep1NumEntities()
    {
        // Move all CirclesRight to NumEntitiesLeft[0], fade them out, and fade caption in simultaneously
        Vector3 targetPosition = NumEntitiesLeft[0].transform.position;
        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Join(CaptionStep1.DOFade(1f, AppearTime));
        foreach (Image circle in CirclesRight)
        {
            moveSequence.Join(circle.transform.DOMove(targetPosition, MoveTime));
            moveSequence.Join(circle.DOFade(0f, MoveTime));
        }
        await moveSequence.ToUniTask();

        // Fade in all NumEntitiesLeft and Cross0 simultaneously
        Sequence appearSequence = DOTween.Sequence();
        foreach (Image image in NumEntitiesLeft)
        {
            appearSequence.Join(image.DOFade(1f, AppearTime));
        }
        appearSequence.Join(Cross0.DOFade(1f, AppearTime));
        await appearSequence.ToUniTask();
    }

    private async UniTask RunStep2CellsPerEntity()
    {
        // Fade CellsPerEntityRight up to MaxCellAlpha and fade caption in simultaneously
        Sequence revealSequence = DOTween.Sequence();
        revealSequence.Join(CaptionStep2.DOFade(1f, AppearTime));
        foreach (Image image in CellsPerEntityRight)
        {
            revealSequence.Join(image.DOFade(MaxCellAlpha, AppearTime));
        }
        await revealSequence.ToUniTask();

        // Move all CellsPerEntityRight to CellsPerEntityLeft[0] and fade them out simultaneously
        Vector3 targetPosition = CellsPerEntityLeft[0].transform.position;
        Sequence moveSequence = DOTween.Sequence();
        foreach (Image image in CellsPerEntityRight)
        {
            moveSequence.Join(image.transform.DOMove(targetPosition, MoveTime));
            moveSequence.Join(image.DOFade(0f, MoveTime));
        }
        await moveSequence.ToUniTask();

        // Fade in all CellsPerEntityLeft and Cross1 simultaneously
        Sequence appearSequence = DOTween.Sequence();
        foreach (Image image in CellsPerEntityLeft)
        {
            appearSequence.Join(image.DOFade(1f, AppearTime));
        }
        appearSequence.Join(Cross1.DOFade(1f, AppearTime));
        await appearSequence.ToUniTask();
    }

    private async UniTask RunStep3EntitiesInCell()
    {
        // Fade CellSingle up to MaxCellAlpha and fade caption in simultaneously
        await DOTween.Sequence()
            .Join(CaptionStep3.DOFade(1f, AppearTime))
            .Join(CellSingle.DOFade(MaxCellAlpha, AppearTime))
            .ToUniTask();

        // Move all EntitiesInCellsRight to EntitiesInCellLeft[0] and fade them out simultaneously
        Vector3 targetPosition = EntitiesInCellLeft[0].transform.position;
        Sequence moveSequence = DOTween.Sequence();
        foreach (Image image in EntitiesInCellsRight)
        {
            moveSequence.Join(image.transform.DOMove(targetPosition, MoveTime));
            moveSequence.Join(image.DOFade(0f, MoveTime));
        }
        await moveSequence.ToUniTask();

        // Fade in all EntitiesInCellLeft and Cross2 simultaneously
        Sequence appearSequence = DOTween.Sequence();
        foreach (Image image in EntitiesInCellLeft)
        {
            appearSequence.Join(image.DOFade(1f, AppearTime));
        }
        appearSequence.Join(Cross2.DOFade(1f, AppearTime));
        appearSequence.Insert(AppearTime, CellSingle.DOFade(0, AppearTime));
        await appearSequence.ToUniTask();
    }

    private async UniTask RunStep4MaxPairs()
    {
        // Move all PairsRight to MaxPairsLeft[0], fade them out, and fade caption in simultaneously
        Vector3 targetPosition = MaxPairsLeft[0].transform.position;
        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Join(CaptionStep4.DOFade(1f, AppearTime));
        foreach (Image image in PairsRight)
        {
            moveSequence.Join(image.transform.DOMove(targetPosition, MoveTime));
            moveSequence.Join(image.DOFade(0f, MoveTime));
        }
        await moveSequence.ToUniTask();

        // Fade in all MaxPairsLeft simultaneously
        Sequence appearSequence = DOTween.Sequence();
        foreach (Image image in MaxPairsLeft)
        {
            appearSequence.Join(image.DOFade(1f, AppearTime));
        }
        await appearSequence.ToUniTask();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
