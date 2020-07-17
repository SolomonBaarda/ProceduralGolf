using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public TerrainChunkManager TerrainChunkManager;

    public bool IsGenerating { get; private set; } = false;

    [Header("Generation settings")]
    public Noise.PerlinSettings HeightMapSettings;
    public MeshGenerator.MeshSettings ChunkMeshSettings;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;


    [Header("Materials")]
    public Material TerrainMaterial;

    private void Update()
    {
        if (Input.GetButtonDown("Submit") || (Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Began))
        {
            Generate();
        }
    }

    public void Generate()
    {
        if (!IsGenerating)
        {
            if (DoRandomSeed)
            {
                Seed = Noise.RandomSeed;
            }

            StartCoroutine(WaitForGenerate(Seed));
        }
    }


    public void Clear()
    {
        if (!IsGenerating)
        {
            TerrainChunkManager.Clear();
        }
    }



    private IEnumerator WaitForGenerate(int seed)
    {
        DateTime before = DateTime.Now;

        IsGenerating = true;

        // Reset the whole HexMap
        TerrainChunkManager.Clear();



        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                GenerateChunk(x, y, seed);
            }
        }


        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;

        yield return null;
    }



    public void GenerateChunk(int x, int y, int seed)
    {
        // Get the chunk number and bounds
        Vector2Int chunk = new Vector2Int(x, y);
        Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

        // Get the height map
        Vector2[,] pointsInChunkToSample = MeshGenerator.CalculatePointsToSampleFrom(chunkBounds, ChunkMeshSettings);
        float[,] heights = Noise.Perlin(HeightMapSettings, seed, pointsInChunkToSample);
        HeightMap heightMap = new HeightMap(heights);

        TerrainChunkManager.AddNewChunk(chunk, heightMap, TerrainMaterial);
    }


    private void SetTilesForChunk(float[,] heightMap, Vector3Int initialTileWorldPos)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        int capacity = width * height;
        float[] heights = new float[capacity];
        Vector3Int[] positions = new Vector3Int[capacity];

        // Loop through each height and calculate its position
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                heights[index] = heightMap[x, y];
                positions[index] = initialTileWorldPos + new Vector3Int(x, y, 0);
            }
        }

        // Set all the tiles
        //HexMap.AddHexagons(positions, heights);
    }





    public class HeightMap
    {
        public float[,] Heights;

        public HeightMap(float[,] heights)
        {
            Heights = heights;
        }
    }




}
