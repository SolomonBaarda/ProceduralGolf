using System;
using UnityEngine;

public static class Noise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    public static float[] GetNoise(NoiseSettings s, int seed, in Vector2 offset, in Vector2 distanceBetweenSamples, int width, int height, out float min, out float max)
    {
        s.ValidateValues();
        FastNoiseLite n = new FastNoiseLite(seed);
        n.SetNoiseType(s.NoiseType);
        n.SetFrequency(s.Frequency);
        n.SetFractalType(s.FractalType);
        n.SetFractalOctaves(s.Octaves);
        n.SetFractalLacunarity(s.Lacunarity);
        n.SetFractalGain(s.Gain);
        n.SetFractalWeightedStrength(s.WeightedStrength);
        n.SetCellularDistanceFunction(s.DistanceFunction);
        n.SetCellularReturnType(s.ReturnType);
        n.SetCellularJitter(s.Jitter);

        float[] noise = new float[width * height];

        min = float.MaxValue;
        max = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                // Calculate the position to sample the noise from
                Vector2 samplePoint = offset + (new Vector2(x, y) * distanceBetweenSamples);
                noise[index] = n.GetNoise(samplePoint.x, samplePoint.y);

                if (noise[index] < min)
                    min = noise[index];

                if (noise[index] > max)
                    max = noise[index];
            }
        }

        return noise;
    }

    public static void NormaliseNoise(ref float[] array, float min, float max)
    {
        float maxMinusMin = max - min;

        for (int i = 0; i < array.Length; i++)
        {
            array[i] = (array[i] - min) / maxMinusMin;
        }
    }


}


