using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public static class MeshGenerator
{





    public static Vector2[,] CalculatePointsToSampleFrom(Bounds chunk, MeshSettings settings)
    {
        settings.ValidateValues();

        Vector2[,] pointsToSample = new Vector2[settings.TotalVertices, settings.TotalVertices];
        Vector2 distanceBetweenVertices = (chunk.max - chunk.min) / settings.TotalVertices;

        // Iterate over each point
        for (int y = 0; y < settings.TotalVertices; y++)
        {
            for (int x = 0; x < settings.TotalVertices; x++)
            {
                // Calculate the point in space to sample from
                pointsToSample[x, y] = (Vector2)chunk.min + (new Vector2(x, y) * distanceBetweenVertices);
            }
        }

        return pointsToSample;
    }







    [System.Serializable]
    [CreateAssetMenu()]
    public class MeshSettings : ISettings
    {
        public int NoiseSamplePointDensity = 16;
        public int TotalVertices;

        public override void ValidateValues()
        {
            NoiseSamplePointDensity = Mathf.Max(NoiseSamplePointDensity, 1);
            // There is always one extra vertex than face
            TotalVertices = NoiseSamplePointDensity + 1;
        }

    }
}
