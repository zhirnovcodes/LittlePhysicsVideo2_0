using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Visual_7_5 : VisualBase
{
    [SerializeField] private List<Cell7_5> Cells;
    [SerializeField] private List<GameObject> Circles;

    private const float AlphaChangeDuration = 0.3f;
    private const float MinAlpha = 0.2f;
    private const float CellMaxAlpha = 0.5f;
    private const float SelectedCircleAlpha = 0.8f;

    private List<Material> CircleMaterials;
    private List<Material> CellMaterials;

    public override void Init()
    {
        CircleMaterials = new List<Material>();
        CellMaterials = new List<Material>();

        // Cache a material instance for each circle and set it to min alpha
        foreach (GameObject circle in Circles)
        {
            Material circleMat = circle.GetComponent<Renderer>().material;
            SetMaterialAlpha(circleMat, MinAlpha);
            CircleMaterials.Add(circleMat);
        }

        // Cache a material instance for each cell and hide all cells, highlights, and elements
        foreach (Cell7_5 cell in Cells)
        {
            Material cellMat = cell.GetComponent<Renderer>().material;
            SetMaterialAlpha(cellMat, 0f);
            CellMaterials.Add(cellMat);

            foreach (Image highlight in cell.Highlights)
            {
                SetImageAlpha(highlight, 0f);
            }

            foreach (Image element in cell.Elements)
            {
                SetImageAlpha(element, 0f);
            }
        }
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Gate on input and animate each cell in sequence
        for (int i = 0; i < Cells.Count; i++)
        {
            // Wait for the user to press Space before showing this cell
            //_ = await controller.WaitNextClicked();

            await RunCell(i);
        }
    }

    private async UniTask RunCell(int cellIndex)
    {
        Cell7_5 cell = Cells[cellIndex];
        Material cellMat = CellMaterials[cellIndex];

        // Fade the cell in to its max alpha
        await cellMat.DOFade(CellMaxAlpha, AlphaChangeDuration).ToUniTask();

        // Fade all referenced circles to selected alpha and all elements to 1 simultaneously
        Sequence fadeInSequence = DOTween.Sequence();
        foreach (int circleIndex in cell.Circles)
        {
            fadeInSequence.Join(CircleMaterials[circleIndex].DOFade(SelectedCircleAlpha, AlphaChangeDuration));
        }
        foreach (Image element in cell.Elements)
        {
            fadeInSequence.Join(element.DOFade(1f, AlphaChangeDuration));
        }
        await fadeInSequence.ToUniTask();

        // Return all referenced circles to min alpha simultaneously
        Sequence fadeOutCirclesSequence = DOTween.Sequence();
        foreach (int circleIndex in cell.Circles)
        {
            fadeOutCirclesSequence.Join(CircleMaterials[circleIndex].DOFade(MinAlpha, AlphaChangeDuration));
        }
        await fadeOutCirclesSequence.ToUniTask();

        // Fade the cell back out
        await cellMat.DOFade(0f, AlphaChangeDuration).ToUniTask();
    }

    private static void SetMaterialAlpha(Material mat, float alpha)
    {
        Color color = mat.color;
        color.a = alpha;
        mat.color = color;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }
}
