using System;
using System.Collections;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public TerrainChunkManager TerrainChunkManager;

    public bool IsGenerating { get; private set; } = false;

    [Header("Settings")]
    public MeshGenerator.MeshSettings MeshSettingsVisual;
    public MeshGenerator.MeshSettings MeshSettingsCollider;
    [Space]
    public Noise.NoiseSettings NoiseSettings_Green;
    public TerrainSettings TerrainSettings_Green;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;


    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;

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

        // Get the vertex points
        Vector3[,] vertices = CalculateVertexPointsForChunk(chunkBounds, TerrainSettings_Green);
        Vector3[,] localVertexPositions = CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
        Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

        // Get the height map
        HeightMapGenerator.HeightMap heightMap = new HeightMapGenerator.HeightMap(Noise.Perlin(NoiseSettings_Green, seed, noiseSamplePoints), localVertexPositions, TerrainSettings_Green);

        TerrainChunkManager.AddNewChunk(chunk, heightMap, MaterialGrass, PhysicsGrass, MeshSettingsVisual, MeshSettingsCollider);
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





    [CreateAssetMenu()]
    public class TerrainSettings : VariablePreset
    {
        public bool UseCurve = false;
        public AnimationCurve HeightDistribution;
        public float HeightMultiplier = 2;

        /// <summary>
        /// Number of Noise sample points taken in each chunk.
        /// </summary>
        public readonly int SamplePointFrequency = 241;
        public int TerrainDivisions => SamplePointFrequency - 1;


        public override void ValidateValues()
        {
            //SamplePointFrequency = Mathf.ClosestPowerOfTwo(Mathf.Max(SamplePointFrequency, 2));
        }

    }


}
