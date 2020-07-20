using System;
using System.Collections;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public TerrainChunkManager TerrainChunkManager;

    public bool IsGenerating { get; private set; } = false;

    [Header("Settings")]
    public MeshGenerator.MeshSettings ChunkMeshSettings;
    [Space]
    public Noise.NoiseSettings NoiseSettings_Green;
    public TerrainSettings TerrainSettings_Green;

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



        for (int y = -4; y <= 4; y++)
        {
            for (int x = -4; x <= 4; x++)
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

        // Get the vertex points
        Vector3[,] vertices = MeshGenerator.CalculateVertexPointsForChunk(chunkBounds, ChunkMeshSettings);
        Vector3[,] localVertexPositions = MeshGenerator.CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
        Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

        // Get the height map
        HeightMap heightMap = new HeightMap(Noise.Perlin(NoiseSettings_Green, seed, noiseSamplePoints), localVertexPositions, TerrainSettings_Green);

        TerrainChunkManager.AddNewChunk(chunk, heightMap, TerrainMaterial, ChunkMeshSettings);
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




    public struct HeightMap
    {
        public float[,] Heights;
        public Vector3[,] LocalVertexPositions;
        public TerrainSettings TerrainSettings;

        public HeightMap(float[,] heights, Vector3[,] vertices, TerrainSettings terrainSettings)
        {
            Heights = heights;
            LocalVertexPositions = vertices;
            TerrainSettings = terrainSettings;

            //DebugMinMax();
        }


        public void DebugMinMax()
        {
            if (Heights != null)
            {
                int width = Heights.GetLength(0), height = Heights.GetLength(1);
                float min = Heights[0, 0], max = Heights[0, 0];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float curr = Heights[x, y];
                        if (curr < min)
                        {
                            min = curr;
                        }
                        if (curr > max)
                        {
                            max = curr;
                        }
                    }
                }

                Debug.Log("min: " + min + " max: " + max);
            }
        }
    }



    [CreateAssetMenu()]
    public class TerrainSettings : VariablePreset
    {
        public bool UseCurve = false;
        public AnimationCurve HeightDistribution;
        public float HeightMultiplier = 2;

        public override void ValidateValues()
        {
        }
    }


}
