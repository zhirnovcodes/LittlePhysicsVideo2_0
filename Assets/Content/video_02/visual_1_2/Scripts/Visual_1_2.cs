using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Visual_1_2 : VisualBase
{
    [SerializeField] private Image LockImage;
    [SerializeField] private Image[] ArrowImages;

    [SerializeField] private List<Image> ArrayElementImages;

    private const float AppearTime = 1.6f;
    private const float ArrayTime = 0.6f;

    private float[] SavedWidths;

    public override void Init()
    {
        SavedWidths = new float[ArrowImages.Length];

        // Position each arrow to span from its start to end placeholder and record the full width
        for (int i = 0; i < ArrowImages.Length; i++)
        {
            float width = ArrowImages[i].rectTransform.sizeDelta.x;
            SavedWidths[i] = width;
        }

        // Collapse all arrow widths to zero
        foreach (Image arrow in ArrowImages)
        {
            Vector2 size = arrow.rectTransform.sizeDelta;
            size.x = 0f;
            arrow.rectTransform.sizeDelta = size;
        }

        // Hide all array element images
        foreach (Image element in ArrayElementImages)
        {
            element.color = new Color(element.color.r, element.color.g, element.color.b, 0f);
        }

        // Hide lock image
        LockImage.color = new Color(LockImage.color.r, LockImage.color.g, LockImage.color.b, 0f);
    }

    public override async UniTask Run(IVisualController controller)
    {
        await RunStep1(controller);

        // Wait for the user to press Space to continue to step 2
        _ = await controller.WaitNextClicked();

        await RunStep2();
    }

    private async UniTask RunStep1(IVisualController controller)
    {
        // Expand arrow 0 to full width and arrow 1 to half width simultaneously
        await DOTween.Sequence()
            .Join(TweenArrowWidth(0, SavedWidths[0], AppearTime))
            .Join(TweenArrowWidth(1, SavedWidths[1] / 2f, AppearTime))
            .ToUniTask();

        // Fade lock in while array elements 0 and 1 appear one by one, all starting simultaneously
        await DOTween.Sequence()
            .Append(ArrayElementImages[0].DOFade(1f, ArrayTime))
            .Append(ArrayElementImages[1].DOFade(1f, ArrayTime))
            .Insert(0f, LockImage.DOFade(1f, AppearTime))
            .ToUniTask();

        _ = await controller.WaitNextClicked();

        // Fade lock out while arrow 1 expands from half to full width, simultaneously
        await DOTween.Sequence()
            .Join(LockImage.DOFade(0f, AppearTime))
            .Join(TweenArrowWidth(1, SavedWidths[1], AppearTime))
            .ToUniTask();

        // Fade array elements 2 and 3 in one by one
        await ArrayElementImages[2].DOFade(1f, ArrayTime).ToUniTask();
        await ArrayElementImages[3].DOFade(1f, ArrayTime).ToUniTask();
    }

    private async UniTask RunStep2()
    {
        // Collapse arrows 0 and 1 to zero width simultaneously
        await DOTween.Sequence()
            .Join(TweenArrowWidth(0, 0f, AppearTime))
            .Join(TweenArrowWidth(1, 0f, AppearTime))
            .ToUniTask();

        // Expand arrows 2 and 3 to their full widths simultaneously
        await DOTween.Sequence()
            .Join(TweenArrowWidth(2, SavedWidths[2], AppearTime))
            .Join(TweenArrowWidth(3, SavedWidths[3], AppearTime))
            .ToUniTask();

        // Fade elements 4,5 and elements 6,7 in one by one within each pair, both pairs running simultaneously
        await UniTask.WhenAll(
            FadeElementsPairAsync(4, 5),
            FadeElementsPairAsync(6, 7)
        );
    }

    private async UniTask FadeElementsPairAsync(int firstIndex, int secondIndex)
    {
        // Fade first element in, then second element in sequentially
        await ArrayElementImages[firstIndex].DOFade(1f, ArrayTime).ToUniTask();
        await ArrayElementImages[secondIndex].DOFade(1f, ArrayTime).ToUniTask();
    }

    private Tweener TweenArrowWidth(int arrowIndex, float targetWidth, float duration)
    {
        RectTransform arrowRect = ArrowImages[arrowIndex].rectTransform;
        float currentHeight = arrowRect.sizeDelta.y;
        return arrowRect.DOSizeDelta(new Vector2(targetWidth, currentHeight), duration);
    }
}
