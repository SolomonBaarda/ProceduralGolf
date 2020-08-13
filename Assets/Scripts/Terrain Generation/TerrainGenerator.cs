using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;
    public List<Hole> GolfHoles = new List<Hole>();

    private UnityAction<TerrainMapGenerator.TerrainMapData, Vector2Int> OnTerrainMapDataCreated;
    private UnityAction<Vector2Int> OnChunkGenerated;
    private UnityAction<List<TerrainChunk>> OnChunkTerrainMapsChanged;
    public UnityAction OnChunksUpdated;

    public bool IsGenerating { get; private set; } = false;
    public bool InitialTerrainGenerated { get; private set; } = false;
    public const int InitialChunksToGenerateRadius = 1;


    [Header("Settings")]
    public MeshSettings MeshSettings;
    [Space]
    public NoiseSettings NoiseSettings_Green;
    public NoiseSettings NoiseSettings_Bunker;
    public NoiseSettings NoiseSettings_Holes;
    public TerrainSettings TerrainSettings_Green;

    [Space]
    public TextureSettings MapSettings;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;

    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;

    [Header("Prefabs")]
    public GameObject GolfHoleFlagPrefab;

    private void Awake()
    {
        OnTerrainMapDataCreated += InstantiateTerrainChunk;

        OnChunkGenerated += CheckChunkAddEdgeNeighbours;
        OnChunkTerrainMapsChanged += CheckUpdatedChunksForHoles;
        OnChunksUpdated += Utils.EMPTY;
    }

    private void OnDestroy()
    {
        OnTerrainMapDataCreated -= InstantiateTerrainChunk;

        OnChunkGenerated -= CheckChunkAddEdgeNeighbours;
        OnChunkTerrainMapsChanged -= CheckUpdatedChunksForHoles;
        OnChunksUpdated -= Utils.EMPTY;
    }

    public void GenerateInitialTerrain()
    {
        if (!IsGenerating)
        {
            if (DoRandomSeed)
            {
                Seed = Noise.RandomSeed;
            }

            StartCoroutine(GenerateInitialArea(Seed));
        }
    }


    public void Clear()
    {
        if (!IsGenerating)
        {
            TerrainChunkManager.Clear();
            GolfHoles.Clear();
        }
    }



    private IEnumerator GenerateInitialArea(int seed)
    {
        DateTime before = DateTime.Now;

        InitialTerrainGenerated = false;
        IsGenerating = true;

        int chunksFromOrigin = Mathf.Abs(InitialChunksToGenerateRadius);

        // Reset the whole terrain map
        TerrainChunkManager.Clear();

        // Generate in that area
        for (int y = -chunksFromOrigin; y <= chunksFromOrigin; y++)
        {
            for (int x = -chunksFromOrigin; x <= chunksFromOrigin; x++)
            {
                TryGenerateChunk(new Vector2Int(x, y), seed);
                yield return null;
            }
        }

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");

        IsGenerating = false;
        InitialTerrainGenerated = true;
    }



    public void TryGenerateChunk(Vector2Int chunk, int seed)
    {
        if (!TerrainChunkManager.TerrainChunkExists(chunk))
        {
            IsGenerating = true;

            // Get the chunk bounds
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            StartChunkGenerationJob(chunk, seed, chunkBounds, TerrainSettings_Green, NoiseSettings_Green, NoiseSettings_Bunker,
                NoiseSettings_Holes, MeshSettings, MapSettings);
        }
    }



    private void CheckChunkAddEdgeNeighbours(Vector2Int pos)
    {
        TerrainChunk newChunk = TerrainChunkManager.GetChunk(pos);

        List<(TerrainChunk, Vector2Int)> relativeNeighbours = new List<(TerrainChunk, Vector2Int)>();

        // Check the 3x3 of nearby chunks
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector2Int relativePos = new Vector2Int(x, y);
                Vector2Int neighbour = pos + relativePos;
                // Don't add its self
                if (!(neighbour.x == pos.x && neighbour.y == pos.y))
                {
                    // Get the neighbour chunk
                    TerrainChunk c = TerrainChunkManager.GetChunk(neighbour);
                    if (c != null)
                    {
                        relativeNeighbours.Add((c, relativePos));
                    }
                }
            }
        }


        // Record which chunks have changed
        List<TerrainChunk> chunksUpdated = new List<TerrainChunk>();

        // Update the edge references for both the new and existing chunk
        foreach ((TerrainChunk, Vector2Int) chunk in relativeNeighbours)
        {
            // New chunk
            TerrainMap.AddEdgeNeighbours(chunk.Item2.x, chunk.Item2.y, ref newChunk.TerrainMap, in chunk.Item1.TerrainMap, out bool needsUpdateA);

            // Existing chunk
            TerrainMap.AddEdgeNeighbours(-chunk.Item2.x, -chunk.Item2.y, ref chunk.Item1.TerrainMap, in newChunk.TerrainMap, out bool needsUpdateB);


            // If there was an update then both chunks need to have their meshes re generated
            if (needsUpdateA || needsUpdateB)
            {
                if (!chunksUpdated.Contains(newChunk))
                {
                    chunksUpdated.Add(newChunk);
                }
                if (!chunksUpdated.Contains(chunk.Item1))
                {
                    chunksUpdated.Add(chunk.Item1);
                }
            }
        }

        // Call the event
        if (chunksUpdated.Count > 0)
        {
            OnChunkTerrainMapsChanged.Invoke(chunksUpdated);
        }
    }


    private void CheckUpdatedChunksForHoles(List<TerrainChunk> chunksUpdated)
    {
        // Create new meshes for each chunk that needs updating
        foreach (TerrainChunk c in chunksUpdated)
        {
            c.MeshData = MeshGenerator.GenerateMeshData(c.TerrainMap);
            c.UpdateVisualMesh(MeshSettings);
            c.UpdateColliderMesh(MeshSettings, true);

            c.RecalculateTexture();
        }

        // Remove all holes that have no vertices
        GolfHoles.RemoveAll((x) => x.Vertices.Count == 0);


        OnChunksUpdated.Invoke();
    }



    public List<Vector2Int> GetAllPossibleNearbyChunks(Vector3 position, float radius)
    {
        // Calculate the centre chunk
        Vector2Int centre = TerrainChunkManager.WorldToChunk(position);
        int chunks = Mathf.RoundToInt(radius / TerrainChunkManager.ChunkSizeWorldUnits);

        List<Vector2Int> nearbyChunks = new List<Vector2Int>();

        // Generate in that area
        for (int y = -chunks; y <= chunks; y++)
        {
            for (int x = -chunks; x <= chunks; x++)
            {
                Vector2Int chunk = new Vector2Int(centre.x + x, centre.y + y);
                nearbyChunks.Add(chunk);
            }
        }

        return nearbyChunks;
    }


    public List<Vector2Int> GetAllNearbyChunks(Vector3 position, float radius)
    {
        // Get all possible
        List<Vector2Int> allPossible = GetAllPossibleNearbyChunks(position, radius);

        // Remove the ones that haven't been generated yet
        allPossible.RemoveAll((x) => TerrainChunkManager.GetChunk(x) == null);

        return allPossible;
    }


    public void TryGenerateNearbyChunks(Vector3 position, float viewDistanceWorldUnits)
    {
        // Get all possible chunks
        List<Vector2Int> nearbyChunks = GetAllPossibleNearbyChunks(position, viewDistanceWorldUnits);

        // Try and generate them
        foreach (Vector2Int chunk in nearbyChunks)
        {
            if (TerrainChunkManager.GetChunk(chunk) == null)
            {
                TryGenerateChunk(chunk, Seed);
            }
        }

        // Set only those chunks to be visible
        TerrainChunkManager.SetVisibleChunks(nearbyChunks);
    }




    private static Vector2[,] ConvertWorldPointsToPerlinSample(Vector3[,] points)
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




    private void InstantiateTerrainChunk(TerrainMapGenerator.TerrainMapData d, Vector2Int chunk)
    {
        TerrainMap terrainMap = new TerrainMap(d);

        Bounds bounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);


        // Generate the mesh data
        MeshGenerator.MeshData meshData = MeshGenerator.GenerateMeshData(terrainMap);


        // Get the holes
        List<Hole> holesInThisChunk = Hole.CalculateHoles(ref terrainMap);
        // Update them all
        foreach (Hole h in holesInThisChunk)
        {
            h.Flag = Instantiate(GolfHoleFlagPrefab, transform);

            h.UpdateHole();

            GolfHoles.Add(h);
        }


        // Create the new chunk
        TerrainChunkManager.AddNewChunk(chunk, bounds, terrainMap, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer, meshData, MeshSettings, MapSettings);

        // Call the event
        OnChunkGenerated.Invoke(chunk);
    }



    private void StartChunkGenerationJob(Vector2Int chunk, int seed, Bounds chunkBounds, TerrainSettings courseSettings, NoiseSettings courseNoiseSettings,
        NoiseSettings bunkerNoiseSettings, NoiseSettings holeNoiseSettings, MeshSettings meshSettings, TextureSettings textureSettings)
    {
        // Create the new job and add all the data it needs
        GenerateChunkJob j = new GenerateChunkJob();
        j.AddSettings(courseSettings, courseNoiseSettings, bunkerNoiseSettings, holeNoiseSettings, meshSettings, textureSettings);
        j.Adddata(chunk, seed, chunkBounds);


        // Create the memory for the result
        NativeArray<TerrainMapGenerator.TerrainMapData> result = new NativeArray<TerrainMapGenerator.TerrainMapData>(1, Allocator.TempJob);
        j.ChunkData = result;

        // Schedule the job
        JobHandle handle = j.Schedule();
        // Wait for it to complete
        handle.Complete();


        // Access the result
        TerrainMapGenerator.TerrainMapData data = result[0];
        // Free the memory
        result.Dispose();


        // Call the event
        OnTerrainMapDataCreated.Invoke(data, chunk);
    }




    private struct GenerateChunkJob : IJob
    {
        public NativeArray<TerrainMapGenerator.TerrainMapData> ChunkData;

        private TerrainSettings courseSettings;
        private NoiseSettings courseNoiseSettings;
        private NoiseSettings bunkerNoiseSettings;
        private NoiseSettings holeNoiseSettings;
        private MeshSettings meshSettings;
        private TextureSettings textureSettings;

        private Vector2Int chunk;
        private int seed;
        private Bounds chunkBounds;

        public void AddSettings(TerrainSettings courseSettings, NoiseSettings courseNoiseSettings, NoiseSettings bunkerNoiseSettings, NoiseSettings holeNoiseSettings,
            MeshSettings meshSettings, TextureSettings textureSettings)
        {
            this.courseSettings = courseSettings;
            this.courseNoiseSettings = courseNoiseSettings;
            this.bunkerNoiseSettings = bunkerNoiseSettings;
            this.holeNoiseSettings = holeNoiseSettings;
            this.meshSettings = meshSettings;
            this.textureSettings = textureSettings;
        }



        public void Adddata(Vector2Int chunk, int seed, Bounds chunkBounds)
        {
            this.chunk = chunk;
            this.seed = seed;
            this.chunkBounds = chunkBounds;
        }





        public void Execute()
        {
            courseSettings.ValidateValues();

            // Get the vertex points
            Vector3[,] vertices = CalculateVertexPointsForChunk(chunkBounds, courseSettings);
            Vector3[,] localVertexPositions = CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
            Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

            int width = vertices.GetLength(0), height = vertices.GetLength(1);

            // Heights
            float[,] heightsRaw = Noise.Perlin(courseNoiseSettings, seed, noiseSamplePoints);

            // Bunkers
            int bunkerSeed = Noise.Seed(seed.ToString());
            float[,] bunkerRaw = Noise.Perlin(bunkerNoiseSettings, bunkerSeed, noiseSamplePoints);

            // Holes
            int holeSeed = Noise.Seed(bunkerSeed.ToString());
            float[,] holesRaw = Noise.Perlin(holeNoiseSettings, holeSeed, noiseSamplePoints);

            // Create masks from the bunkers and holes
            float[,] bunkerShapeMask = new float[width, height], holeShapeMask = new float[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Bunker
                    if (bunkerRaw[x, y] >= courseSettings.BunkerNoiseThresholdMinMax.x && bunkerRaw[x, y] <= courseSettings.BunkerNoiseThresholdMinMax.y)
                    {
                        bunkerShapeMask[x, y] = bunkerRaw[x, y];
                    }
                    else
                    {
                        bunkerShapeMask[x, y] = TerrainMapGenerator.PointData.Empty;
                    }

                    // Hole
                    if (holesRaw[x, y] >= courseSettings.HoleNoiseThresholdMinMax.x && holesRaw[x, y] <= courseSettings.HoleNoiseThresholdMinMax.y)
                    {
                        holeShapeMask[x, y] = holesRaw[x, y];
                    }
                    else
                    {
                        holeShapeMask[x, y] = TerrainMapGenerator.PointData.Empty;
                    }
                }
            }


            // Now assign the data to the result
            ChunkData[0] = TerrainMapGenerator.Generate(width, height, localVertexPositions, chunkBounds.center, heightsRaw, bunkerShapeMask, holeShapeMask, courseSettings);
        }
    }

}
