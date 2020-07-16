using System;
using System.IO.Pipes;
using UnityEngine;

public class MyNoise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    public const float MoveAmount = 0.33f;
    private readonly System.Random r;

    public MyNoise(System.Random r)
    {
        this.r = r;
    }


    /// <summary>
    /// Fills an array randomly with 0 and 1.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="percentFillChance"></param>
    /// <returns></returns>
    public int[,] FillRandomInt(int width, int height, int percentFillChance)
    {
        // Assign memory for the array
        int[,] array = new int[width, height];

        // Loop over full array
        for (int y = 0; y < array.GetLength(1); y++)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                // Set the value
                array[x, y] = r.Next(0, 100) <= percentFillChance ? 1 : 0;
            }
        }

        return array;
    }


    /// <summary>
    /// Fills an array with values between 0 and 1;
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="minimum"></param>
    /// <returns></returns>
    public float[,] FillRandomFloats(int width, int height, float minimum)
    {
        // Assign memory for the array
        float[,] array = new float[width, height];

        // Loop over full array
        for (int y = 0; y < array.GetLength(1); y++)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                // Set the value
                float f = (float)r.NextDouble();
                array[x, y] = f <= minimum ? f : 0;
            }
        }

        return array;
    }




    /// <summary>
    /// Smooths an array of floats. Repeats for times. Smooths an area of 2 * radius + 1 for each cell (centered on that cell).
    /// </summary>
    /// <param name="array"></param>
    /// <param name="times"></param>
    /// <param name="radius"></param>
    public static void SmoothFloatMap(ref float[,] array, int times, int radius = 1)
    {
        if (times > 0)
        {
            // Smooth it multiple times
            for (int i = 0; i < times; i++)
            {
                array = SmoothFloatMapOnce(in array, radius);
            }
        }
    }


    private static float[,] SmoothFloatMapOnce(in float[,] array, int extents)
    {
        // Force the offset to always be at least 1
        extents = Mathf.Abs(extents);
        if (extents < 0)
        {
            extents = 1;
        }

        float[,] newArray = new float[array.GetLength(0), array.GetLength(1)];

        for (int y = 0; y < array.GetLength(1); y++)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                // Don't smooth if already at limit
                if (array[x, y] != 0 && array[x, y] != 1)
                {
                    // Get the average of the nearby cells
                    float total = 0;
                    float cellsCounted = 0;
                    // Loop over nearby cells
                    for (int j = y - extents; j <= y + extents; j++)
                    {
                        for (int i = x - extents; i <= x + extents; i++)
                        {
                            // Inside array
                            if (j >= 0 && j < array.GetLength(1) && i >= 0 && i < array.GetLength(0))
                            {
                                // Don't include central cell
                                if (j != x && i != y)
                                {
                                    total += array[i, j];
                                    cellsCounted++;
                                }
                            }
                        }
                    }

                    float average = total / cellsCounted;

                    newArray[x, y] = Mathf.SmoothStep(array[x, y], average, MoveAmount);
                }
            }
        }

        return newArray;
    }





    public static void ErodeIntMask(ref int[,] array, int times)
    {
        if (times > 0)
        {
            // Smooth it multiple times
            for (int i = 0; i < times; i++)
            {
                array = ErodeIntMaskOnce(in array);
            }
        }
    }


    private static int[,] ErodeIntMaskOnce(in int[,] array)
    {
        int[,] newArray = new int[array.GetLength(0), array.GetLength(1)];

        for (int y = 0; y < array.GetLength(1); y++)
        {
            for (int x = 0; x < array.GetLength(0); x++)
            {
                // Get the nearby cell count
                int neighbourCells = GetSurroundingWallCount(in array, x, y);

                // Smooth edges
                if (neighbourCells > 4)
                {
                    newArray[x, y] = 1;
                }
                else if (neighbourCells < 4)
                {
                    newArray[x, y] = 0;
                }
            }
        }

        return newArray;
    }


    private static int GetSurroundingWallCount(in int[,] array, int gridX, int gridY)
    {
        int wallCount = 0;
        // Loop through the 3x3 of cells
        for (int y = gridY - 1; y <= gridY + 1; y++)
        {
            for (int x = gridX - 1; x <= gridX + 1; x++)
            {
                // Ensure within the array
                if (x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1))
                {
                    // Ensure it doesn't count its self
                    if (x != gridX || y != gridY)
                    {
                        wallCount += array[x, y];
                    }
                }
                else
                {
                    // Don't count the edges as walls - we want an island
                    //wallCount++;
                }
            }
        }

        return wallCount;
    }


    public static int[,] GetCircularIntMask(Grid grid, Vector3Int islandCentreCell, int radius, int maskWidth, int maskHeight)
    {
        int[,] mask = new int[maskWidth, maskHeight];
        Vector2 centre = grid.CellToWorld(islandCentreCell);

        // Loop over full array
        for (int y = 0; y < mask.GetLength(1); y++)
        {
            for (int x = 0; x < mask.GetLength(0); x++)
            {
                // Calculate the distance between centre and point
                float distance = Vector2.Distance(grid.CellToWorld(new Vector3Int(x, y, 0)), centre);
                mask[x, y] = distance <= radius ? 1 : 0;
            }
        }

        return mask;
    }




    public static void ApplyMask(ref int[,] array, in int[,] mask)
    {
        if (array != null && mask != null)
        {
            // Ensure same size
            if (array.GetLength(0) == mask.GetLength(0) && array.GetLength(1) == mask.GetLength(1))
            {
                // Loop over full array
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    for (int x = 0; x < array.GetLength(0); x++)
                    {
                        // Apply the mask
                        array[x, y] = mask[x, y] != 0 ? array[x, y] : 0;
                    }
                }
            }
            else
            {
                Debug.LogError("Trying to apply mask of different size.");
            }
        }

    }


    public static void ApplyMask(ref float[,] array, in int[,] mask)
    {
        if (array != null && mask != null)
        {
            // Ensure same size
            if (array.GetLength(0) == mask.GetLength(0) && array.GetLength(1) == mask.GetLength(1))
            {
                // Loop over full array
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    for (int x = 0; x < array.GetLength(0); x++)
                    {
                        // Apply the mask
                        array[x, y] = mask[x, y] != 0 ? array[x, y] : 0;
                    }
                }
            }
            else
            {
                Debug.LogError("Trying to apply mask of different size.");
            }
        }
    }





    public static int[,] InvertIntMask(in int[,] mask)
    {
        int[,] inverted = new int[mask.GetLength(0), mask.GetLength(1)];

        // Loop over full array
        for (int y = 0; y < mask.GetLength(1); y++)
        {
            for (int x = 0; x < mask.GetLength(0); x++)
            {
                // Invert the mask
                inverted[x, y] = mask[x, y] != 0 ? 0 : 1;
            }
        }

        return inverted;
    }





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






    public static float[,] PerlinFull(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode, Vector2 sampleCentre)
    {
        float[,] perlin = new float[width, height];

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleheight = 0;
        float amplitude = 1;
        float frequency = 1;

        // Fill the array with random position offsets
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + offset.x + sampleCentre.x;
            float offsetY = r.Next(-100000, 100000) - offset.y + sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            // Calculate the max possible values
            maxPossibleheight += amplitude;
            amplitude *= persistance;
        }

        // Ensure the scale is always above zero
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        // Record the max and min values
        float maxLocalNoiseHeight = float.MinValue, minLocalNoiseHeight = float.MaxValue;

        // Calculate half the dimensions
        float halfWidth = width / 2, halfHeight = height / 2;


        // Fill with noise
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // Loop through each octave
                for (int octave = 0; octave < octaves; octave++)
                {
                    // Calculate the position to sample the noise from
                    float sampleX = (x - halfWidth + octaveOffsets[octave].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[octave].y) / scale * frequency;

                    // Get perlin in the range -1 to 1
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // Update the max values
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                // Assign the perlin value
                perlin[x, y] = noiseHeight;

                // Normalise the height globally
                if (normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (perlin[x, y] + 1) / (maxLocalNoiseHeight / 0.9f);
                    perlin[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
                // Do a basic mode - very approximite
                else if (normalizeMode == NormalizeMode.Basic)
                {
                    perlin[x, y] = Mathf.Clamp01((perlin[x, y] / 2f) + 0.5f);
                }
            }
        }

        if (normalizeMode == NormalizeMode.Local)
        {
            // Now ensure the noise is within the range of 0 to 1
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Normalize it using local values
                    perlin[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, perlin[x, y]);
                }
            }
        }

        return perlin;
    }


    public enum NormalizeMode { Local, Global, Basic }




    /// <summary>
    /// Calculates a Perlin value at position additionalOffset using settings. Note values are not always between 0 and 1.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="seed"></param>
    /// <param name="additionalOffset"></param>
    /// <returns></returns>
    public static float Perlin(PerlinSettings settings, int seed, Vector2 additionalOffset = default)
    {
        float perlin;

        System.Random r = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        // Fill the array with random position offsets
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = r.Next(-100000, 100000) + settings.offset.x + additionalOffset.x;
            float offsetY = r.Next(-100000, 100000) - settings.offset.y + additionalOffset.y;
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



    [System.Serializable]
    public class PerlinSettings
    {
        public float scale = 50;

        public int octaves = 4;
        [Range(0, 1)]
        public float persistance = .6f;
        public float lacunarity = 2;

        public Vector2 offset;

        public void ValidateValues()
        {
            scale = Mathf.Max(scale, 0.01f);
            octaves = Mathf.Max(octaves, 1);
            lacunarity = Mathf.Max(lacunarity, 1);
            persistance = Mathf.Clamp01(persistance);
        }
    }


}


