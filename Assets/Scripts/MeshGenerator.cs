using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{




    public static Vector2[,] CalculatePointsToSampleFrom(Bounds chunk, MeshSettings settings)
    {
        return null;
    }



    [System.Serializable] [CreateAssetMenu()]
    public class MeshSettings : ISettings
    {
        public int NoiseSamplePointDensity = 16;

        public override void ValidateValues()
        {
            NoiseSamplePointDensity = Mathf.Max(NoiseSamplePointDensity, 1);
        }

    }
}
