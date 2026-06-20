using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Visual_4_4 : VisualBase
{
    [SerializeField] private Image ImpulseArrowImage;
    [SerializeField] private Image ContactPointImage;

    [SerializeField] private Image ContactPointTextImage;
    [SerializeField] private Image ImpulseTextImage;

    private const float AppearTime = 1f;

    public override void Init()
    {
        // Hide the impulse arrow image
        ImpulseArrowImage.color = new Color(ImpulseArrowImage.color.r, ImpulseArrowImage.color.g, ImpulseArrowImage.color.b, 0f);

        // Hide the contact point image
        ContactPointImage.color = new Color(ContactPointImage.color.r, ContactPointImage.color.g, ContactPointImage.color.b, 0f);

        // Hide the contact point text image
        ContactPointTextImage.color = new Color(ContactPointTextImage.color.r, ContactPointTextImage.color.g, ContactPointTextImage.color.b, 0f);

        // Hide the impulse text image
        ImpulseTextImage.color = new Color(ImpulseTextImage.color.r, ImpulseTextImage.color.g, ImpulseTextImage.color.b, 0f);
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade the contact point text image and contact point image in simultaneously
        await DOTween.Sequence()
            .Join(ContactPointTextImage.DOFade(1f, AppearTime))
            .Join(ContactPointImage.DOFade(1f, AppearTime))
            .ToUniTask();

        // Wait for the user to press Space to continue
        _ = await controller.WaitNextClicked();

        // Fade the impulse text image and impulse arrow image in simultaneously
        await DOTween.Sequence()
            .Join(ImpulseTextImage.DOFade(1f, AppearTime))
            .Join(ImpulseArrowImage.DOFade(1f, AppearTime))
            .ToUniTask();
    }
}
