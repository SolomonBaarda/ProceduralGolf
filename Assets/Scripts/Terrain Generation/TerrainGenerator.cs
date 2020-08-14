using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TerrainGenerator : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;
    public List<Hole> GolfHoles = new List<Hole>();

    public UnityAction<Vector2Int> OnChunkGenerated;
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
    public TextureSettings Texture_GroundSettings;
    public TextureSettings Texture_MapSettings;

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
        OnChunkGenerated += CheckChunkAddEdgeNeighbours;
        OnChunkTerrainMapsChanged += CheckUpdatedChunksForHoles;
        OnChunksUpdated += Utils.EMPTY;
    }

    private void OnDestroy()
    {
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

            GenerateInitialArea(Seed);
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



    private IEnumerator WaitUntiInitialChunksGenerated(List<Vector2Int> initialChunks, DateTime start)
    {
        while (initialChunks.Count > 0)
        {
            // Remove all chunks that have been generated
            initialChunks.RemoveAll((x) => TerrainChunkManager.TerrainChunkExists(x));

            yield return null;
        }


        InitialTerrainGenerated = true;

        Debug.Log("Generated in " + (DateTime.Now - start).TotalSeconds.ToString("0.0") + " seconds.");
    }

    private void GenerateInitialArea(int seed)
    {
        DateTime before = DateTime.Now;

        InitialTerrainGenerated = false;
        IsGenerating = true;

        int chunksFromOrigin = Mathf.Abs(InitialChunksToGenerateRadius);

        // Reset the whole terrain map
        TerrainChunkManager.Clear();

        List<Vector2Int> initialChunks = new List<Vector2Int>();

        // Generate in that area
        for (int y = -chunksFromOrigin; y <= chunksFromOrigin; y++)
        {
            for (int x = -chunksFromOrigin; x <= chunksFromOrigin; x++)
            {
                Vector2Int chunk = new Vector2Int(x, y);
                initialChunks.Add(chunk);

                // Generate the chunk
                TryGenerateChunk(chunk, seed);
            }
        }

        // Wait until the chunks have been generated
        StartCoroutine(WaitUntiInitialChunksGenerated(initialChunks, before));
    }



    public void TryGenerateChunk(Vector2Int chunk, int seed)
    {
        if (!TerrainChunkManager.TerrainChunkExists(chunk))
        {
            IsGenerating = true;

            // Get the chunk bounds
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            ThreadedDataRequester.RequestData
            (
                () => GenerateTerrainMap(chunk, seed, chunkBounds),
                OnTerrainMapGenerated
            );
        }
    }



    private void OnTerrainMapGenerated(object terrainMapObject)
    {
        TerrainMap terrainMap = (TerrainMap)terrainMapObject;

        // Get the bunkers
        List<Hole> holesInThisChunk = Hole.CalculateHoles(ref terrainMap);

        // Update them all
        foreach (Hole h in holesInThisChunk)
        {
            h.Flag = Instantiate(GolfHoleFlagPrefab, transform);

            h.UpdateHole();

            GolfHoles.Add(h);
        }

        // Create the new chunk
        TerrainChunkManager.AddNewChunk(terrainMap.Chunk, terrainMap.Bounds, terrainMap, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer, MeshSettings, Texture_GroundSettings);

        OnChunkGenerated.Invoke(terrainMap.Chunk);
        IsGenerating = false;
    }



    private TerrainMap GenerateTerrainMap(Vector2Int chunk, int seed, Bounds chunkBounds)
    {
        // Get the vertex points
        Vector3[,] vertices = CalculateVertexPointsForChunk(chunkBounds, TerrainSettings_Green);
        Vector3[,] localVertexPositions = CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
        Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

        int width = vertices.GetLength(0), height = vertices.GetLength(1);

        // Heights
        float[,] heightsRaw = Noise.Perlin(NoiseSettings_Green, seed, noiseSamplePoints);

        // Bunkers
        int bunkerSeed = Noise.Seed(seed.ToString());
        float[,] bunkerRaw = Noise.Perlin(NoiseSettings_Bunker, bunkerSeed, noiseSamplePoints);

        // Holes
        int holeSeed = Noise.Seed(bunkerSeed.ToString());
        float[,] holesRaw = Noise.Perlin(NoiseSettings_Holes, holeSeed, noiseSamplePoints);

        // Create masks from the bunkers and holes
        float[,] bunkerShapeMask = new float[width, height], holeShapeMask = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Bunker
                if (bunkerRaw[x, y] >= TerrainSettings_Green.BunkerNoiseThresholdMinMax.x && bunkerRaw[x, y] <= TerrainSettings_Green.BunkerNoiseThresholdMinMax.y)
                {
                    bunkerShapeMask[x, y] = bunkerRaw[x, y];
                }
                else
                {
                    bunkerShapeMask[x, y] = TerrainMap.Point.Empty;
                }

                // Hole
                if (holesRaw[x, y] >= TerrainSettings_Green.HoleNoiseThresholdMinMax.x && holesRaw[x, y] <= TerrainSettings_Green.HoleNoiseThresholdMinMax.y)
                {
                    holeShapeMask[x, y] = holesRaw[x, y];
                }
                else
                {
                    holeShapeMask[x, y] = TerrainMap.Point.Empty;
                }
            }
        }

        TerrainMap terrainMap = new TerrainMap(chunk, width, height, localVertexPositions, chunkBounds, heightsRaw, bunkerShapeMask, holeShapeMask, TerrainSettings_Green);


        // Get the terrain map
        return terrainMap;
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
            newChunk.TerrainMap.AddEdgeNeighbours(chunk.Item2.x, chunk.Item2.y, ref chunk.Item1.TerrainMap, out bool needsUpdateA);

            // Existing chunk
            chunk.Item1.TerrainMap.AddEdgeNeighbours(-chunk.Item2.x, -chunk.Item2.y, ref newChunk.TerrainMap, out bool needsUpdateB);


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
            c.RecalculateMesh(MeshSettings);
            c.RecalculateTexture(Texture_GroundSettings);
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
