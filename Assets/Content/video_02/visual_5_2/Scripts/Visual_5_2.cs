using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_5_2 : VisualBase
{
    [SerializeField] private Image Selected0;
    [SerializeField] private TMP_Text Count0;

    [SerializeField] private RectTransform Arrow0_0;
    [SerializeField] private RectTransform Arrow0_1;
    [SerializeField] private float Arrow0_1InitWidth;
    [SerializeField] private RectTransform Arrow0_1_Error;

    [SerializeField] private Image LockImage;

    [SerializeField] private RectTransform Arrow1_0;
    [SerializeField] private RectTransform Arrow1_1;

    [SerializeField] private Image Selected1;
    [SerializeField] private Image Selected2;

    [SerializeField] private TMP_Text Count1;
    [SerializeField] private TMP_Text Count2;

    private const float AppearTime = 0.8f;

    private float SavedArrow0_0Width;
    private float SavedArrow0_1Width;
    private float SavedArrow1_0Width;
    private float SavedArrow1_1Width;

    private Image Arrow0_1_ErrorImage;

    public override void Init()
    {
        // Cache the Image component on the error indicator RectTransform
        Arrow0_1_ErrorImage = Arrow0_1_Error.GetComponent<Image>();

        // Hide Selected0
        SetAlpha(Selected0, 0f);

        // Hide the error indicator
        SetAlpha(Arrow0_1_ErrorImage, 0f);

        // Hide the lock icon
        SetAlpha(LockImage, 0f);

        // Save each arrow's natural width, then zero it out
        SavedArrow0_0Width = Arrow0_0.sizeDelta.x;
        Arrow0_0.sizeDelta = new Vector2(0f, Arrow0_0.sizeDelta.y);

        SavedArrow0_1Width = Arrow0_1.sizeDelta.x;
        Arrow0_1.sizeDelta = new Vector2(0f, Arrow0_1.sizeDelta.y);

        SavedArrow1_0Width = Arrow1_0.sizeDelta.x;
        Arrow1_0.sizeDelta = new Vector2(0f, Arrow1_0.sizeDelta.y);

        SavedArrow1_1Width = Arrow1_1.sizeDelta.x;
        Arrow1_1.sizeDelta = new Vector2(0f, Arrow1_1.sizeDelta.y);

        // Hide Selected1 and Selected2
        SetAlpha(Selected1, 0f);
        SetAlpha(Selected2, 0f);
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep1ShowArrowsAndLock();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep2ShowArrow1Group();
    }

    private async UniTask RunStep1ShowArrowsAndLock()
    {
        // Grow Arrow0_0 to its saved width and Arrow0_1 to its initial (locked) width simultaneously
        await DOTween.Sequence()
            .Join(Arrow0_0.DOSizeDelta(new Vector2(SavedArrow0_0Width, Arrow0_0.sizeDelta.y), AppearTime))
            .Join(Arrow0_1.DOSizeDelta(new Vector2(Arrow0_1InitWidth, Arrow0_1.sizeDelta.y), AppearTime))
            .ToUniTask();

        // Fade the lock icon in
        await LockImage.DOFade(1f, AppearTime).ToUniTask();

        // Fade Selected0 highlight in
        await Selected0.DOFade(1f, AppearTime).ToUniTask();

        // Update the count label
        Count0.text = "6";

        // Shrink Arrow0_0 back to zero
        await Arrow0_0.DOSizeDelta(new Vector2(0f, Arrow0_0.sizeDelta.y), AppearTime).ToUniTask();

        // Fade the lock out and simultaneously grow Arrow0_1 to its full saved width
        await DOTween.Sequence()
            .Join(LockImage.DOFade(0f, AppearTime))
            .Join(Arrow0_1.DOSizeDelta(new Vector2(SavedArrow0_1Width, Arrow0_1.sizeDelta.y), AppearTime))
            .ToUniTask();

        // Flash the error indicator: fade in then fade back out
        await DOTween.Sequence()
            .Append(Arrow0_1_ErrorImage.DOFade(1f, AppearTime * 0.5f))
            .Append(Arrow0_1_ErrorImage.DOFade(0f, AppearTime * 0.5f))
            .ToUniTask();

        // Shrink Arrow0_1 back to zero
        await Arrow0_1.DOSizeDelta(new Vector2(0f, Arrow0_1.sizeDelta.y), AppearTime).ToUniTask();
    }

    private async UniTask RunStep2ShowArrow1Group()
    {
        // Grow Arrow1_0 and Arrow1_1 to their saved widths simultaneously
        await DOTween.Sequence()
            .Join(Arrow1_0.DOSizeDelta(new Vector2(SavedArrow1_0Width, Arrow1_0.sizeDelta.y), AppearTime))
            .Join(Arrow1_1.DOSizeDelta(new Vector2(SavedArrow1_1Width, Arrow1_1.sizeDelta.y), AppearTime))
            .ToUniTask();

        // Fade Selected1 and Selected2 highlights in simultaneously
        await DOTween.Sequence()
            .Join(Selected1.DOFade(1f, AppearTime))
            .Join(Selected2.DOFade(1f, AppearTime))
            .ToUniTask();

        // Update count labels for both highlighted elements
        Count1.text = "5";
        Count2.text = "4";
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
