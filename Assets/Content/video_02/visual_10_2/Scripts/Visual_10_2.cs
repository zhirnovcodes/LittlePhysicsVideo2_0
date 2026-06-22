using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_10_2 : VisualBase
{
    [SerializeField] private List<Image> Highlights;
    [SerializeField] private Image LastElement;
    [SerializeField] private TMP_Text LastText;
    [SerializeField] private TMP_Text Count;
    [SerializeField] private Image Lock;
    [SerializeField] private RectTransform Arrow1;
    [SerializeField] private RectTransform Arrow2;
    [SerializeField] private float MidWidth;

    private const float AppearTime = 0.8f;
    private const float ElementTime = 0.4f;

    private float SavedArrow1Width;
    private float SavedArrow2Width;

    public override void Init()
    {
        // Hide all highlights and the last element
        foreach (Image highlight in Highlights)
        {
            SetAlpha(highlight, 0f);
        }
        SetAlpha(LastElement, 0f);

        // Save each arrow's natural width, then collapse both to zero
        SavedArrow1Width = Arrow1.sizeDelta.x;
        Arrow1.sizeDelta = new Vector2(0f, Arrow1.sizeDelta.y);

        SavedArrow2Width = Arrow2.sizeDelta.x;
        Arrow2.sizeDelta = new Vector2(0f, Arrow2.sizeDelta.y);

        // Hide the last text and the lock icon
        SetAlpha(LastText, 0f);
        SetAlpha(Lock, 0f);
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Grow Arrow1 to its saved width, Arrow2 to MidWidth, and fade the lock in — all simultaneously
        await DOTween.Sequence()
            .Join(Arrow1.DOSizeDelta(new Vector2(SavedArrow1Width, Arrow1.sizeDelta.y), AppearTime))
            .Join(Arrow2.DOSizeDelta(new Vector2(MidWidth, Arrow2.sizeDelta.y), AppearTime))
            .Join(Lock.DOFade(1f, AppearTime))
            .ToUniTask();

        // Flash each highlight from invisible to visible and back, one by one
        await RunHighlightSequence();

        // Fade in the last element image and its label simultaneously
        await DOTween.Sequence()
            .Join(LastElement.DOFade(1f, AppearTime))
            .Join(LastText.DOFade(1f, AppearTime))
            .ToUniTask();

        // Update the count label to reflect the new total
        Count.text = "4";

        // Collapse Arrow1, grow Arrow2 to its saved width, and fade the lock out — all simultaneously
        await DOTween.Sequence()
            .Join(Arrow1.DOSizeDelta(new Vector2(0f, Arrow1.sizeDelta.y), AppearTime))
            .Join(Arrow2.DOSizeDelta(new Vector2(SavedArrow2Width, Arrow2.sizeDelta.y), AppearTime))
            .Join(Lock.DOFade(0f, AppearTime))
            .ToUniTask();
    }

    private async UniTask RunHighlightSequence()
    {
        // Fade each highlight to full opacity then back to transparent, one after another
        Sequence highlightSequence = DOTween.Sequence();
        float offset = 0f;
        foreach (Image highlight in Highlights)
        {
            highlightSequence
                .Insert(offset, highlight.DOFade(1f, ElementTime * 0.5f))
                .Insert(offset + ElementTime * 0.5f, highlight.DOFade(0f, ElementTime * 0.5f));
            offset += ElementTime;
        }
        await highlightSequence.ToUniTask();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }

    private static void SetAlpha(TMP_Text text, float alpha)
    {
        text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
    }
}
