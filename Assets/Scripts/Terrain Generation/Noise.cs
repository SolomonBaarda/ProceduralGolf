using System;
using System.Threading.Tasks;
using UnityEngine;

public static class Noise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    // Simplex
    // float scale = 1, int octaves = 3, float persistance = 0.6f,
    // float lacunarity = 2


    public static float[] GetSimplex(NoiseSettings s, int seed, Vector3 offset, in Vector3[] samplePoints, int width, int height, out float min, out float max)
    {
        return GetNoise(s, seed, offset, samplePoints, width, height, FastNoiseLite.NoiseType.OpenSimplex2, out min, out max);
    }

    public static float[] GetPerlin(NoiseSettings s, int seed, Vector3 offset, in Vector3[] samplePoints, int width, int height, out float min, out float max)
    {
        return GetNoise(s, seed, offset, samplePoints, width, height, FastNoiseLite.NoiseType.Perlin, out min, out max);
    }

    private static float[] GetNoise(NoiseSettings s, int seed, Vector3 offset, in Vector3[] samplePoints, int width, int height, FastNoiseLite.NoiseType noiseType, out float min, out float max)
    {
        s.ValidateValues();
        FastNoiseLite n = new FastNoiseLite(seed);
        n.SetNoiseType(noiseType);

        float[] noise = new float[samplePoints.Length];

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[s.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < s.octaves; i++)
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
                int index = y * height + x;
                float amplitude = 1, frequency = 1, raw = 0;

                // Loop through each octave
                for (int octave = 0; octave < s.octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + new Vector2(offset.x + samplePoints[index].x, offset.z + samplePoints[index].z) / s.scale * frequency;

                    // Get perlin in the range -1 to 1
                    raw += n.GetNoise(sample.x, sample.y) * amplitude;

                    amplitude *= s.persistance;
                    frequency *= s.lacunarity;
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
        Vector2[] octaveOffsets = new Vector2[s.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < s.octaves; i++)
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
                int index = y * height + x;
                float amplitude = 1, frequency = 1, raw = 0;

                // Loop through each octave
                for (int octave = 0; octave < s.octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + new Vector2(offset.x + samplePoints[index].x, offset.z + samplePoints[index].z) / s.scale * frequency;

                    // Get perlin in the range -1 to 1
                    raw += n.GetNoise(sample.x, sample.y) * amplitude;

                    amplitude *= s.persistance;
                    frequency *= s.lacunarity;
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


