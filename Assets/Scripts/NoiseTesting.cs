using UnityEngine;

public class NoiseTesting : MonoBehaviour
{
    public Vector2 size;
    [Min(0)]
    public int divisions;
    public Vector2 offset;

    private Vector2 samplePointGap;
    private Vector2[,] samplePoints;
    private float[,] values;

    public float scale;


    private void OnValidate()
    {
        samplePointGap = size / divisions;

        samplePoints = new Vector2[divisions, divisions];
        for (int y = 0; y < divisions; y++)
        {
            for (int x = 0; x < divisions; x++)
            {
                samplePoints[x, y] = new Vector2(x * samplePointGap.x, y * samplePointGap.y);
            }
        }

        values = Noise.Cellular(scale, offset, 0, samplePoints);
    }


    private void OnDrawGizmosSelected()
    {
        if (values != null && samplePoints != null)
        {
            for (int y = 0; y < divisions - 1; y++)
            {
                for (int x = 0; x < divisions - 1; x++)
                {
                    Gizmos.color = Color.Lerp(Color.white, Color.black, values[x, y]);

                    Gizmos.DrawCube(samplePoints[x, y], samplePointGap);
                }
            }
        }

    }
}
