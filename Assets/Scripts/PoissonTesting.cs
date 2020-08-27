using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoissonTesting : MonoBehaviour
{

    public float radius = 1;
    public Vector2 Size = Vector2.one;
    public Vector3 Centre = Vector3.zero;
    public int rejectionSamples = 30;
    public float displayRadius = 0.25f;

    private Bounds bounds;
    private List<Vector3> points;

    void OnValidate()
    {
        bounds = new Bounds(Centre, new Vector3(Size.x, 0, Size.y));
        points = PoissonDiscSampling.GenerateWorldPoints(radius, bounds, rejectionSamples);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        if (points != null)
        {
            foreach (Vector3 point in points)
            {
                Gizmos.DrawSphere(point, displayRadius);
            }
        }
    }
}