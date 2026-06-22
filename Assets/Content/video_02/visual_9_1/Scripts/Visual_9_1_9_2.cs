using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Visual_9_1_9_2 : VisualBase
{
    [SerializeField] private Image Formula1;
    [SerializeField] private Image Formula2;
    [SerializeField] private TMP_Text Denominator;
    [SerializeField] private Image Coef;
    [SerializeField] private List<Image> Cross;

    private const float AppearTime = 0.8f;
    private const float CrossDelta = 0.4f;

    public override void Init()
    {
        // Hide both formula images
        SetAlpha(Formula1, 0f);
        SetAlpha(Formula2, 0f);

        // Hide the denominator text
        Denominator.alpha = 0f;

        // Hide the coefficient image
        SetAlpha(Coef, 0f);

        // Hide every cross image
        foreach (Image cross in Cross)
        {
            SetAlpha(cross, 0f);
        }
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade Formula1 in, then Formula2, then Denominator sequentially
        await Formula1.DOFade(1f, AppearTime).ToUniTask();
        await Formula2.DOFade(1f, AppearTime).ToUniTask();
        await Denominator.DOFade(1f, AppearTime).ToUniTask();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade each cross in one by one with a staggered delay of CrossDelta between each
        Sequence crossSequence = DOTween.Sequence();
        float delay = 0f;
        foreach (Image cross in Cross)
        {
            crossSequence.Insert(delay, cross.DOFade(1f, AppearTime));
            delay += CrossDelta;
        }
        await crossSequence.ToUniTask();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade the coefficient image in
        await Coef.DOFade(1f, AppearTime).ToUniTask();
    }

    private static void SetAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
