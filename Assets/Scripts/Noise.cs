using System;
using System.IO.Pipes;
using UnityEngine;

public static class Noise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    public static float[,] DistributeEvenly(in float[,] array, float min, float max)
    {
        if (min < max)
        {
            if (array != null)
            {
                // Set the min and max to be the first element
                float currentMin = array[0, 0], currentMax = array[0, 0];

                // Find the current minumum and maximum
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    for (int x = 0; x < array.GetLength(0); x++)
                    {
                        currentMin = array[x, y] < currentMin ? array[x, y] : currentMin;
                        currentMax = array[x, y] > currentMax ? array[x, y] : currentMax;
                    }
                }

                // Use the formula y = mx + c
                float m = (max - min) / (currentMax - currentMin);
                float c = min - currentMin * m;

                // Apply the formula to all elements
                float[,] distributed = new float[array.GetLength(0), array.GetLength(1)];
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    for (int x = 0; x < array.GetLength(0); x++)
                    {
                        distributed[x, y] = m * array[x, y] + c;
                    }
                }

                // Return the new array
                return distributed;
            }
            else
            {
                throw new Exception("Array is null.");
            }
        }
        else
        {
            throw new Exception("Minimum must be less than the maximum.");
        }

    }

















    /// <summary>
    /// Calculates a Perlin value at position additionalOffset using settings. Note values are not always between 0 and 1.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="seed"></param>
    /// <param name="sampleStartPoint"></param>
    /// <returns></returns>
    public static float PerlinPoint(PerlinSettings settings, int seed, Vector2 sampleStartPoint = default)
    {
        float perlin;

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x + sampleStartPoint.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y + sampleStartPoint.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        // Loop through each octave
        for (int octave = 0; octave < settings.octaves; octave++)
        {
            // Calculate the position to sample the noise from
            float sampleX = octaveOffsets[octave].x / settings.scale * frequency;
            float sampleY = octaveOffsets[octave].y / settings.scale * frequency;

            // Get perlin in the range -1 to 1
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= settings.persistance;
            frequency *= settings.lacunarity;
        }

        // Assign the perlin value and normalize it roughly
        perlin = (noiseHeight / 2.5f) + 0.5f;

        return perlin;
    }


    public static float[,] Perlin(PerlinSettings settings, int seed, int width, int height, Vector2 sampleStartPoint, Vector2 incrementDistance)
    {
        float[,] perlin = new float[width, height];
        settings.ValidateValues();

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x + sampleStartPoint.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y + sampleStartPoint.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                // Loop through each octave
                for (int octave = 0; octave < settings.octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    float sampleX = octaveOffsets[octave].x + (x * incrementDistance.x) / settings.scale * frequency;
                    float sampleY = octaveOffsets[octave].y + (y * incrementDistance.y) / settings.scale * frequency;

                    // Get perlin in the range -1 to 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                // Assign the perlin value and normalize it roughly
                perlin[x, y] = (noiseHeight / 2.5f) + 0.5f;
            }
        }

        return perlin;
    }



    public static float[,] Perlin(PerlinSettings settings, int seed, Vector2[,] samplePoints)
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
                    float sampleX = octaveOffsets[octave].x + samplePoints[x,y].x / settings.scale * frequency;
                    float sampleY = octaveOffsets[octave].y + samplePoints[x, y].y / settings.scale * frequency;

                    // Get perlin in the range -1 to 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    totalPerlinForIndex += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                // Assign the perlin value and normalize it roughly
                perlin[x, y] = totalPerlinForIndex;
            }
        }

        return perlin;
    }







    [System.Serializable] [CreateAssetMenu()]
    public class PerlinSettings : ISettings
    {
        public float scale = 50;

        public int octaves = 4;
        [Range(0, 1)]
        public float persistance = .5f;
        public float lacunarity = 2;

        public Vector2 offset = Vector2.zero;

        public override void ValidateValues()
        {
            scale = Mathf.Max(scale, 0.01f);
            octaves = Mathf.Max(octaves, 1);
            lacunarity = Mathf.Max(lacunarity, 1);
            persistance = Mathf.Clamp01(persistance);
        }
    }


}


