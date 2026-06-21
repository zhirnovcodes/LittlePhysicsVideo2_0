using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Visual_7_4 : VisualBase
{
    [Serializable]
    public class Content
    {
        public Image Element;
        public int CircleIndex;
    }

    [Serializable]
    public class Cell
    {
        public GameObject CellObject;
        public List<Content> Contents;
    }

    [Serializable]
    public class Circle
    {
        public GameObject CircleObject;
        public Image Highlight;
        public List<Cell> Cells;
    }

    [SerializeField] private List<Circle> Circles;

    private const float AlphaChangeDuration = 0.3f;
    private const float MinAlpha = 0.2f;
    private const float MaxHighlightAlpha = 0.4f;
    private const float CellMaxAlpha = 0.2f;
    private const float SelectedCircleAlpha = 0.8f;

    private List<Material> CircleMaterials;
    private List<List<Material>> CellMaterials;

    public override void Init()
    {
        CircleMaterials = new List<Material>();
        CellMaterials = new List<List<Material>>();

        for (int i = 0; i < Circles.Count; i++)
        {
            Circle circle = Circles[i];

            // Instantiate the circle material, replace it on the renderer, and set to min alpha
            Renderer circleRenderer = circle.CircleObject.GetComponent<Renderer>();
            Material circleMat = circleRenderer.material;
            circleRenderer.material = circleMat;
            SetMaterialAlpha(circleMat, MinAlpha);
            CircleMaterials.Add(circleMat);

            // Hide the highlight image
            SetImageAlpha(circle.Highlight, 0f);

            // Instantiate one material per cell, replace it on the renderer, and hide each cell
            List<Material> cellMats = new List<Material>();
            foreach (Cell cell in circle.Cells)
            {
                Renderer cellRenderer = cell.CellObject.GetComponent<Renderer>();
                Material cellMat = cellRenderer.material;
                cellRenderer.material = cellMat;
                SetMaterialAlpha(cellMat, 0f);
                cellMats.Add(cellMat);

                // Hide every content element belonging to this cell
                foreach (Content content in cell.Contents)
                {
                    SetImageAlpha(content.Element, 0f);
                }
            }
            CellMaterials.Add(cellMats);
        }
    }

    public override async UniTask Run(IVisualController controller)
    {
        // Animate each circle in sequence, gated by user input before each one
        for (int i = 0; i < Circles.Count; i++)
        {
            // Wait for the user to press Space to begin this circle
            _ = await controller.WaitNextClicked();

            await RunCircle(i);
        }
    }

    private async UniTask RunCircle(int circleIndex)
    {
        Circle circle = Circles[circleIndex];
        Material circleMat = CircleMaterials[circleIndex];

        // Fade the circle to full alpha and raise the highlight simultaneously
        await DOTween.Sequence()
            .Join(circleMat.DOFade(1f, AlphaChangeDuration))
            .Join(circle.Highlight.DOFade(MaxHighlightAlpha, AlphaChangeDuration))
            .ToUniTask();

        // Animate each cell in order
        for (int j = 0; j < circle.Cells.Count; j++)
        {
            await RunCell(circle.Cells[j], CellMaterials[circleIndex][j]);
        }

        // Fade the circle back to min alpha and hide the highlight simultaneously
        await DOTween.Sequence()
            .Join(circleMat.DOFade(MinAlpha, AlphaChangeDuration))
            .Join(circle.Highlight.DOFade(0f, AlphaChangeDuration))
            .ToUniTask();
    }

    private async UniTask RunCell(Cell cell, Material cellMat)
    {
        // Fade the cell in to its max alpha
        await cellMat.DOFade(CellMaxAlpha, AlphaChangeDuration).ToUniTask();

        // Animate each content item in sequence
        foreach (Content content in cell.Contents)
        {
            await RunContent(content);
        }

        // Fade the cell back out
        await cellMat.DOFade(0f, AlphaChangeDuration).ToUniTask();
    }

    private async UniTask RunContent(Content content)
    {
        Material referencedMat = CircleMaterials[content.CircleIndex];

        // Raise the referenced circle to selected alpha and fade the element in simultaneously
        await DOTween.Sequence()
            .Join(referencedMat.DOFade(SelectedCircleAlpha, AlphaChangeDuration))
            .Join(content.Element.DOFade(1f, AlphaChangeDuration))
            .ToUniTask();

        // Return the referenced circle to min alpha
        await referencedMat.DOFade(MinAlpha, AlphaChangeDuration).ToUniTask();
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
