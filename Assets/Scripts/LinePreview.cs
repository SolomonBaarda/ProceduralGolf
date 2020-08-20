using UnityEngine;

public class LinePreview : MonoBehaviour
{
    private LineRenderer LineRenderer;
    private Vector3[] Points = new Vector3[2];

    public Gradient LineColour;
    [Min(0)] public float LineWidth = 0.05f;
    public Material LineMaterial;



    private void Awake()
    {
        // Add the line renderer
        LineRenderer = gameObject.AddComponent<LineRenderer>();

        LineRenderer.widthCurve = AnimationCurve.Linear(0f, LineWidth, 1f, LineWidth);

        LineRenderer.material = LineMaterial;
        LineRenderer.colorGradient = LineColour;
    }



    public void SetPoints(Vector3 start, Vector3 end)
    {
        Points[0] = start;
        Points[1] = end;

        LineRenderer.SetPositions(Points);
    }


    private void OnEnable()
    {
        LineRenderer.enabled = true;
    }
    private void OnDisable()
    {
        LineRenderer.enabled = false;
    }

}
