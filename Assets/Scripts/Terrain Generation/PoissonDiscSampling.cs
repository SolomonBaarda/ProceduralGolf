using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiscSampling
{
    // https://github.com/SebLague/Poisson-Disc-Sampling

    public static List<Vector2> GenerateLocalPoints(float radius, Vector2 sampleRegionSize, int seed, int numSamplesBeforeRejection = 30)
    {
        float cellSize = radius / Mathf.Sqrt(2);

        // Create grid and storage for the points
        int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
        List<Vector2> points = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>
        {
            sampleRegionSize / 2
        };

        // Seed the random
        System.Random r = new System.Random(seed);

        while (spawnPoints.Count > 0)
        {
            // Choose a random point to sample against
            int spawnIndex = r.Next(0, spawnPoints.Count);
            Vector2 spawnCentre = spawnPoints[spawnIndex];
            bool candidateAccepted = false;


            // Try to find a valid new position
            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                // Calculate the new possible position
                float angle = (float)r.NextDouble() * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 candidate = spawnCentre + (dir * ((float)r.NextDouble() * radius + radius));

                // Check if it is a valid position
                if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
                {
                    // Accept it if it is 
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                    candidateAccepted = true;
                    break;
                }
            }

            // Remove it if not
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        return points;
    }

    static bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
    {
        if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
        {
            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 2);
            int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

            // Check the 5x5 grid for any other points nearby
            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    int pointIndex = grid[x, y] - 1;
                    if (pointIndex != -1)
                    {
                        float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                        if (sqrDst < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
}