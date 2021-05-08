using System;
using System.Threading.Tasks;
using UnityEngine;

public static class NewNoise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();


    public static TerrainMap GetRawTerrainMap(int seed, int size, Vector3Int origin,
        float scale = 1, int octaves = 3, float persistance = 0.6f, float lacunarity = 2)
    {
        FastNoiseLite noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        float[] heightMap = new float[size * size];
        Vector3Int[] positions = new Vector3Int[size * size];


        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + origin.x;
            float offsetY = r.Next(-100000, 100000) - origin.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        float min = noise.GetNoise(0, 0), max = min;
        for (int yOffset = 0; yOffset < size; yOffset++)
        {
            for (int xOffset = 0; xOffset < size; xOffset++)
            {
                int x = origin.x + xOffset, y = origin.y + yOffset;
                int index = yOffset * size + xOffset;

                positions[index] = new Vector3Int(x, y, 0);


                //float height = noise.GetNoise(x, y);



                float amplitude = 1;
                float frequency = 1;

                float height = 0;

                // Loop through each octave
                for (int octave = 0; octave < octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    Vector2 sample = octaveOffsets[octave] + new Vector2(x, y) / scale * frequency;

                    // Get perlin in the range -1 to 1
                    height += noise.GetNoise(sample.x, sample.y) * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }





                if (height < min)
                    min = height;
                if (height > max)
                    max = height;

                heightMap[index] = height;

            }
        }

        return new TerrainMap() { Heights = heightMap, Positions = positions, MinHeight = min, MaxHeight = max };
    }



    public static void NormaliseTerrainMap(ref TerrainMap t, float min, float max)
    {
        float maxMinusMin = max - min;
        for (int i = 0; i < t.Heights.Length; i++)
        {
            t.Heights[i] = (t.Heights[i] - min) / maxMinusMin;
        }
    }


    /*
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




    public static float[,] Cellular(float scale, Vector2 offset, int seed, Vector2[,] samplePoints)
    {
        int width = samplePoints.GetLength(0), height = samplePoints.GetLength(1);
        float[,] noise = new float[width, height];

        // Get the fast noise object
        FastNoise n = new FastNoise(seed);
        // Set some settings for it
        n.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural);

        Parallel.For(0, height, y =>
        {
            Parallel.For(0, width, x =>
            {
                // Calculate the position to sample the noise from
                Vector2 sample = offset + samplePoints[x, y] / scale;

                noise[x, y] = n.GetCellular(sample.x, sample.y);
            });
        });

        return noise;
    }
    */


    public class TerrainMap
    {
        public Vector3Int[] Positions;
        public float[] Heights;

        public float MinHeight, MaxHeight;
    }

}


