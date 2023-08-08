using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LinePreview : MonoBehaviour
{
    [SerializeField]
    private LineRenderer LineRenderer;

    public Gradient LineColour;
    [Min(0)] public float LineWidth = 0.05f;
    public Material LineMaterial;
    public int OrderInLayer = 0;



    private void Awake()
    {
        LineRenderer.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        LineRenderer.widthMultiplier = LineWidth;

        LineRenderer.material = LineMaterial;
        LineRenderer.colorGradient = LineColour;

        LineRenderer.sortingOrder = OrderInLayer;
    }

    public void SetPoints(Vector3[] points)
    {
        LineRenderer.positionCount = points.Length;
        LineRenderer.SetPositions(points);
    }

    public void UpdateLineWidth(float width)
    {
        LineRenderer.widthMultiplier = width;
    }



}
