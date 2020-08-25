using UnityEngine;

public class LinePreview : MonoBehaviour
{
    private LineRenderer LineRenderer;
    private Vector3[] Points = new Vector3[2];

    public Gradient LineColour;
    [Min(0)] public float LineWidth = 0.05f;
    [Min(0)] public float LineLength = 1;
    public Material LineMaterial;



    private void Awake()
    {
        // Add the line renderer
        LineRenderer = gameObject.AddComponent<LineRenderer>();

        LineRenderer.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        LineRenderer.widthMultiplier = LineWidth;

        LineRenderer.material = LineMaterial;
        LineRenderer.colorGradient = LineColour;
    }



    public void SetPoints(Vector3 start, Vector3 directionNormal)
    {
        Points[0] = start;
        Points[1] = start + (directionNormal * LineLength);

        LineRenderer.SetPositions(Points);
    }

    public void UpdateLineWidth(float width)
    {
        LineRenderer.widthMultiplier = width;
    }


}
