using System;
using System.Threading.Tasks;
using UnityEngine;

public static class Noise
{
    public static int RandomSeed => Seed(Environment.TickCount.ToString());
    public static int Seed(string seed) => seed.GetHashCode();








    public static bool[,] PerlinMask(NoiseSettings settings, int seed, Vector2 thresholdMinMax, Vector2[,] samplePoints)
    {
        int width = samplePoints.GetLength(0), height = samplePoints.GetLength(1);
        bool[,] mask = new bool[width, height];
        settings.ValidateValues();


        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        int octaves = settings.octaves;


        Parallel.For(0, height, y =>
        {
            Parallel.For(0, width, x =>
            {
                float amplitude = 1;
                float frequency = 1;
                float totalPerlinForIndex = 0;

                // Loop through each octave
                for (int octave = 0; octave < octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + samplePoints[x, y] / settings.scale * frequency;

                    // Get perlin in the range -1 to 1
                    float perlinValue = Mathf.PerlinNoise(sample.x, sample.y) * 2 - 1;
                    totalPerlinForIndex += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                // Assign the perlin value and normalize it roughly
                float value = (totalPerlinForIndex / 2.5f) + 0.5f;
                mask[x, y] = value >= thresholdMinMax.x && value <= thresholdMinMax.y;
            });
        });

        return mask;
    }




    public static float[,] Perlin(NoiseSettings settings, int seed, Vector2[,] samplePoints)
    {
        int width = samplePoints.GetLength(0), height = samplePoints.GetLength(1);
        float[,] perlin = new float[width, height];
        settings.ValidateValues();


        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        int octaves = settings.octaves;


        Parallel.For(0, height, y =>
        {
            Parallel.For(0, width, x =>
            {
                float amplitude = 1;
                float frequency = 1;
                float totalPerlinForIndex = 0;

                // Loop through each octave
                for (int octave = 0; octave < octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + samplePoints[x, y] / settings.scale * frequency;

                    // Get perlin in the range -1 to 1
                    float perlinValue = Mathf.PerlinNoise(sample.x, sample.y) * 2 - 1;
                    totalPerlinForIndex += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                // Assign the perlin value and normalize it roughly
                perlin[x, y] = (totalPerlinForIndex / 2.5f) + 0.5f;
            });
        });

        return perlin;
    }



    public static float[,] PerlinOld(in NoiseSettings settings, int seed, in Vector2[,] samplePoints)
    {
        int width = samplePoints.GetLength(0), height = samplePoints.GetLength(1);
        float[,] perlin = new float[width, height];
        settings.ValidateValues();

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float totalPerlinForIndex = 0;

                // Loop through each octave
                for (int octave = 0; octave < settings.octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + samplePoints[x, y] / settings.scale * frequency;

                    // Get perlin in the range -1 to 1
                    float perlinValue = Mathf.PerlinNoise(sample.x, sample.y) * 2 - 1;
                    totalPerlinForIndex += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                // Assign the perlin value and normalize it roughly
                perlin[x, y] = (totalPerlinForIndex / 2.5f) + 0.5f;
            }
        }

        return perlin;
    }





}


