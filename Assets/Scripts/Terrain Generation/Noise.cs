using System;
using UnityEngine;

public static class Noise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    public static float[] GetNoise(NoiseSettings s, int seed, in Vector2 offset, in Vector2 distanceBetweenSamples, int width, int height, ref float min, ref float max)
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

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                // Calculate the position to sample the noise from
                Vector2 samplePoint = offset + new Vector2(x * distanceBetweenSamples.x, y * distanceBetweenSamples.y);
                noise[index] = n.GetNoise(samplePoint.x, samplePoint.y);

                if (noise[index] < min)
                    min = noise[index];
                if (noise[index] > max)
                    max = noise[index];
            }
        }

        return noise;
    }

    private static float[] GetNoiseOLD(NoiseSettings s, int seed, Vector3 offset, in Vector3[] samplePoints, int width, int height, FastNoiseLite.NoiseType noiseType, out float min, out float max)
    {
        s.ValidateValues();
        FastNoiseLite n = new FastNoiseLite(seed);
        n.SetNoiseType(noiseType);
        n.SetFractalType(FastNoiseLite.FractalType.None);
        n.SetFrequency(0.005f);

        float[] noise = new float[samplePoints.Length];

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[s.Octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < s.Octaves; i++)
        {
            octaveOffsets[i] = new Vector2(r.Next(-100000, 100000), r.Next(-100000, 100000));
        }

        min = n.GetNoise(0, 0);
        max = min;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float amplitude = 1, frequency = 1, raw = 0;

                // Loop through each octave
                for (int octave = 0; octave < s.Octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + (new Vector2(offset.x + samplePoints[index].x, offset.z + samplePoints[index].z) / s.Frequency * frequency);

                    // Get noise in the range -1 to 1
                    raw += n.GetNoise(sample.x, sample.y) * amplitude;

                    //amplitude *= s.Persistance;
                    frequency *= s.Lacunarity;
                }

                if (raw < min)
                    min = raw;
                if (raw > max)
                    max = raw;

                noise[index] = raw;
            }
        }

        return noise;
    }

    public static bool[] GetPerlinMask(NoiseSettings s, int seed, Vector3 offset, in Vector3[] samplePoints, int width, int height, Vector2 thresholdMinMax, out float min, out float max)
    {
        return GetMask(s, seed, offset, samplePoints, width, height, thresholdMinMax, FastNoiseLite.NoiseType.Perlin, out min, out max);
    }

    private static bool[] GetMask(NoiseSettings s, int seed, Vector3 offset, in Vector3[] samplePoints, int width, int height, Vector2 thresholdMinMax, FastNoiseLite.NoiseType noiseType, out float min, out float max)
    {
        s.ValidateValues();
        FastNoiseLite n = new FastNoiseLite(seed);
        n.SetNoiseType(noiseType);

        bool[] noise = new bool[samplePoints.Length];

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[s.Octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < s.Octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + offset.x;
            float offsetY = r.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        min = n.GetNoise(0, 0);
        max = min;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float amplitude = 1, frequency = 1, raw = 0;

                // Loop through each octave
                for (int octave = 0; octave < s.Octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + (new Vector2(offset.x + samplePoints[index].x, offset.z + samplePoints[index].z) / s.Frequency * frequency);

                    // Get perlin in the range -1 to 1
                    raw += n.GetNoise(sample.x, sample.y) * amplitude;

                    //amplitude *= s.Persistance;
                    frequency *= s.Lacunarity;
                }

                if (raw < min)
                    min = raw;
                if (raw > max)
                    max = raw;

                noise[index] = raw >= thresholdMinMax.x && raw <= thresholdMinMax.y;
            }
        }

        return noise;
    }



    public static void GetMinMax(in float[] array, out float min, out float max)
    {
        min = array[0];
        max = min;
        foreach (float f in array)
        {
            if (f < min)
                min = f;
            if (f > max)
                max = f;
        }
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


