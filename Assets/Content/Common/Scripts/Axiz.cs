using UnityEngine;

public class Axiz : MonoBehaviour
{
    public RectTransform X;
    public RectTransform Y;
    public RectTransform Z;

    public Transform Cube;

    [Range(0,1)]
    public float T;

    public void SetT(float t)
    {
        const float maxWidth = 1024;
        const float maxScale = 0.2f;

        Vector3 maxScaleVector = Vector3.one * maxScale;

        var width = maxWidth * t;
        var scale = maxScaleVector * t;

        Cube.localScale = scale;

        var size = X.sizeDelta;
        size.x = width;

        X.sizeDelta = size;
        Y.sizeDelta = size;
        Z.sizeDelta = size;
    }

    private void OnValidate()
    {
        if (X == null || Y == null || Z == null || Cube == null)
        {
            return;
        }

        SetT(T);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
