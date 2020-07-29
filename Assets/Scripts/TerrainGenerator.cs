using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public TerrainChunkManager TerrainChunkManager;

    public bool IsGenerating { get; private set; } = false;

    [Header("Settings")]
    public MeshSettings MeshSettingsVisual;
    public MeshSettings MeshSettingsCollider;
    public bool UseSameMesh = true;
    [Space]
    public NoiseSettings NoiseSettings_Green;
    public TerrainSettings TerrainSettings_Green;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;


    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;




    public void GenerateInitialTerrain()
    {
        if (!IsGenerating)
        {
            if (DoRandomSeed)
            {
                Seed = Noise.RandomSeed;
            }

            GenerateInitialArea(Seed, 2);
        }
    }


    public void Clear()
    {
        if (!IsGenerating)
        {
            TerrainChunkManager.Clear();
        }
    }



    private void GenerateInitialArea(int seed, int chunksFromOrigin)
    {
        DateTime before = DateTime.Now;

        IsGenerating = true;
        chunksFromOrigin = Mathf.Abs(chunksFromOrigin);

        // Reset the whole HexMap
        TerrainChunkManager.Clear();


        // Generate in that area
        for (int y = -chunksFromOrigin; y <= chunksFromOrigin; y++)
        {
            for (int x = -chunksFromOrigin; x <= chunksFromOrigin; x++)
            {
                TryGenerateChunk(new Vector2Int(x, y), seed);
            }
        }

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }



    public void TryGenerateChunk(Vector2Int chunk, int seed)
    {
        if (!TerrainChunkManager.TerrainChunkExists(chunk))
        {
            // Get the chunk bounds
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            // Get the vertex points
            Vector3[,] vertices = CalculateVertexPointsForChunk(chunkBounds, TerrainSettings_Green);
            Vector3[,] localVertexPositions = CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
            Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

            // Get the height map
            HeightMapGenerator.HeightMap heightMap = new HeightMapGenerator.HeightMap(Noise.Perlin(NoiseSettings_Green, seed, noiseSamplePoints), localVertexPositions, TerrainSettings_Green);

            TerrainChunkManager.AddNewChunk(chunk, heightMap, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer, MeshSettingsVisual, MeshSettingsCollider, UseSameMesh);
        }
    }





    public void CheckNearbyChunks(Vector3 position, float viewDistanceWorldUnits)
    {
        // Calculate the centre chunk
        Vector2Int centre = TerrainChunkManager.WorldToChunk(position);
        int chunks = Mathf.RoundToInt(viewDistanceWorldUnits / TerrainChunkManager.ChunkSizeWorldUnits);

        List<Vector2Int> nearbyChunks = new List<Vector2Int>();

        // Generate in that area
        for (int y = -chunks; y <= chunks; y++)
        {
            for (int x = -chunks; x <= chunks; x++)
            {
                Vector2Int chunk = new Vector2Int(centre.x + x, centre.y + y);
                TryGenerateChunk(chunk, Seed);
                nearbyChunks.Add(chunk);
            }
        }

        // Set only those chunks to be visible
        TerrainChunkManager.SetVisibleChunks(nearbyChunks);
    }




    private Vector2[,] ConvertWorldPointsToPerlinSample(Vector3[,] points)
    {
        int width = points.GetLength(0), height = points.GetLength(1);
        Vector2[,] points2D = new Vector2[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                points2D[x, y] = new Vector2(points[x, y].x, points[x, y].z);
            }
        }

        return points2D;
    }




    public static Vector3 CalculateDistanceBetweenVertices(Bounds b, int divisions)
    {
        return (b.max - b.min) / divisions;
    }





    public static Vector3[,] CalculateVertexPointsForChunk(in Bounds chunk, TerrainSettings settings)
    {
        settings.ValidateValues();

        Vector3[,] roughVertices = new Vector3[settings.SamplePointFrequency, settings.SamplePointFrequency];
        Vector3 distanceBetweenVertices = CalculateDistanceBetweenVertices(chunk, settings.TerrainDivisions);

        // Iterate over each point
        for (int y = 0; y < settings.SamplePointFrequency; y++)
        {
            for (int x = 0; x < settings.SamplePointFrequency; x++)
            {
                // Calculate the 3d point
                roughVertices[x, y] = chunk.min + new Vector3(x * distanceBetweenVertices.x, distanceBetweenVertices.y, y * distanceBetweenVertices.z);
            }
        }

        return roughVertices;
    }

    public static Vector3[,] CalculateLocalVertexPointsForChunk(in Vector3[,] worldPoints, in Vector3 centre)
    {
        int width = worldPoints.GetLength(0), height = worldPoints.GetLength(1);
        Vector3[,] localPositions = new Vector3[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                localPositions[x, y] = worldPoints[x, y] - centre;
            }
        }

        return localPositions;
    }




}
