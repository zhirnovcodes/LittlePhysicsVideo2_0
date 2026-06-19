using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Visual_3_1 : VisualBase
{
    [SerializeField] private Image Sphere1Image;
    [SerializeField] private Image Sphere2Image;
    [SerializeField] private Image Sphere3Image;

    [SerializeField] private MonoBehaviour Sphere1Script;
    [SerializeField] private MonoBehaviour Sphere2Script;
    [SerializeField] private MonoBehaviour Sphere3Script;

    [SerializeField] private Image GridImage;
    [SerializeField] private RectTransform PairsListImage;
    [SerializeField] private Image HighlightImage;
    [SerializeField] private Image[] ArrowImages;

    private const float AppearTime = 0.8f;
    private const float BlinkValue = 0.8f;
    private const float BlinkTime = 0.2f;

    private float SavedPairsListHeight;

    public override void Init()
    {
        // Hide the grid
        GridImage.color = new Color(GridImage.color.r, GridImage.color.g, GridImage.color.b, 0f);

        // Hide the highlight
        HighlightImage.color = new Color(HighlightImage.color.r, HighlightImage.color.g, HighlightImage.color.b, 0f);

        // Save the design height of PairsListImage then collapse it to zero
        SavedPairsListHeight = PairsListImage.sizeDelta.y;
        PairsListImage.sizeDelta = new Vector2(PairsListImage.sizeDelta.x, 0f);

        // Hide all arrow images
        foreach (Image arrow in ArrowImages)
        {
            arrow.color = new Color(arrow.color.r, arrow.color.g, arrow.color.b, 0f);
        }
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space before disabling sphere scripts
        _ = await controller.WaitNextClicked();

        DisableSphereScripts();

        // Wait for the user to press Space before starting the grid / highlight / pairs animation
        _ = await controller.WaitNextClicked();

        await RunStep1Appear();

        // Wait for the user to press Space before hiding the grid and dimming sphere 3
        _ = await controller.WaitNextClicked();

        await RunStep2GridOut();

        // Wait for the user to press Space before revealing the arrows
        _ = await controller.WaitNextClicked();

        await RunStep3ArrowsIn();
    }

    private void DisableSphereScripts()
    {
        // Disable all three sphere MonoBehaviours
        Sphere1Script.enabled = false;
        Sphere2Script.enabled = false;
        Sphere3Script.enabled = false;
    }

    private async UniTask RunStep1Appear()
    {
        // Fade the grid in
        await GridImage.DOFade(1f, AppearTime).ToUniTask();

        // Fade the highlight in
        await HighlightImage.DOFade(1f, AppearTime).ToUniTask();

        // Start an infinite pulse on the highlight and continue without waiting
        HighlightImage.DOFade(BlinkValue, BlinkTime).SetLoops(-1, LoopType.Yoyo);

        // Expand PairsListImage height to its design height
        await PairsListImage.DOSizeDelta(new Vector2(PairsListImage.sizeDelta.x, SavedPairsListHeight), AppearTime).ToUniTask();
    }

    private async UniTask RunStep2GridOut()
    {
        // Stop the infinite highlight blink and fade it out together with the grid
        HighlightImage.DOKill();
        await DOTween.Sequence()
            .Join(GridImage.DOFade(0f, AppearTime))
            .Join(HighlightImage.DOFade(0f, AppearTime))
            .ToUniTask();

        // Dim sphere 3 to a near-transparent state
        await Sphere3Image.DOFade(0.1f, AppearTime).ToUniTask();
    }

    private async UniTask RunStep3ArrowsIn()
    {
        // Fade all arrow images in simultaneously
        var sequence = DOTween.Sequence();
        foreach (Image arrow in ArrowImages)
        {
            sequence.Join(arrow.DOFade(1f, AppearTime));
        }
        await sequence.ToUniTask();
    }
}
