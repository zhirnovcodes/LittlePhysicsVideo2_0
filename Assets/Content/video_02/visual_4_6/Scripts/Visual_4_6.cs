using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Visual_4_6 : VisualBase
{
    [SerializeField] private RectTransform LastElement;
    [SerializeField] private List<Image> SelectedBackgrounds;
    [SerializeField] private List<Image> Circles;
    [SerializeField] private List<MonoBehaviour> CirclesBehaviours;

    private const float AppearTime = 0.3f;
    private const float ViewDelayTime = 0.3f;
    private const float MinAlpha = 0.2f;

    private float SavedLastElementWidth;
    private Dictionary<int, int> CircleMapping;

    public override void Init()
    {
        // Save the current width of LastElement then collapse it to zero
        SavedLastElementWidth = LastElement.sizeDelta.x;
        LastElement.sizeDelta = new Vector2(0f, LastElement.sizeDelta.y);

        // Hide all selected backgrounds
        foreach (Image background in SelectedBackgrounds)
        {
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0f);
        }
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep1CircleChain();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep2MultiHighlight();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        await RunStep3Final();
    }

    private async UniTask RunStep1CircleChain()
    {
        // Disable all circle MonoBehaviours
        foreach (MonoBehaviour behaviour in CirclesBehaviours)
        {
            behaviour.enabled = false;
        }

        // Map each background index to its associated circle index
        CircleMapping = new Dictionary<int, int> { { 0, 0 }, { 1, 0 }, { 2, 1 }, { 3, 2 } };

        // Dim all circles to MinAlpha simultaneously
        var dimSequence = DOTween.Sequence();
        foreach (Image circle in Circles)
        {
            dimSequence.Join(circle.DOFade(MinAlpha, AppearTime));
        }
        await dimSequence.ToUniTask();

        // Cycle through each background/circle pair sequentially in key order
        for (int key = 0; key <= 3; key++)
        {
            int circleIndex = CircleMapping[key];

            // Fade the paired circle and background in together
            await DOTween.Sequence()
                .Join(Circles[circleIndex].DOFade(1f, AppearTime))
                .Join(SelectedBackgrounds[key].DOFade(1f, AppearTime))
                .ToUniTask();

            // Hold at full alpha for ViewDelayTime
            await UniTask.WaitForSeconds(ViewDelayTime);

            // Fade the paired circle and background back out together
            await DOTween.Sequence()
                .Join(Circles[circleIndex].DOFade(MinAlpha, AppearTime))
                .Join(SelectedBackgrounds[key].DOFade(0f, AppearTime))
                .ToUniTask();
        }
    }

    private async UniTask RunStep2MultiHighlight()
    {
        // Collect all background keys whose mapped circle is circle 0
        var keysForCircle0 = new List<int>();
        foreach (var pair in CircleMapping)
        {
            if (pair.Value == 0)
            {
                keysForCircle0.Add(pair.Key);
            }
        }

        // Fade circle 0 and all associated backgrounds in simultaneously
        var appearSequence = DOTween.Sequence()
            .Join(Circles[0].DOFade(1f, AppearTime));
        foreach (int key in keysForCircle0)
        {
            appearSequence.Join(SelectedBackgrounds[key].DOFade(1f, AppearTime));
        }
        await appearSequence.ToUniTask();

        // Hold at full alpha for ViewDelayTime
        await UniTask.WaitForSeconds(ViewDelayTime);

        // Fade circle 0 and all associated backgrounds back out simultaneously
        var fadeSequence = DOTween.Sequence()
            .Join(Circles[0].DOFade(MinAlpha, AppearTime));
        foreach (int key in keysForCircle0)
        {
            fadeSequence.Join(SelectedBackgrounds[key].DOFade(0f, AppearTime));
        }
        await fadeSequence.ToUniTask();
    }

    private async UniTask RunStep3Final()
    {
        // Fade circle 0 and circle 3 in and restore LastElement width simultaneously
        await DOTween.Sequence()
            .Join(Circles[0].DOFade(1f, AppearTime))
            .Join(Circles[3].DOFade(1f, AppearTime))
            .Join(LastElement.DOSizeDelta(new Vector2(SavedLastElementWidth, LastElement.sizeDelta.y), AppearTime))
            .ToUniTask();
    }
}
