using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;
    public HashSet<Hole> GolfHoles = new HashSet<Hole>();

    private List<NeedsUpdating> chunksThatNeedUpdating = new List<NeedsUpdating>();
    public const float ChunkWaitSecondsBeforeUpdate = 0.25f;

    public UnityAction<Vector2Int> OnChunkGenerated;
    public UnityAction OnChunksUpdated;


    public bool IsGenerating { get; private set; } = false;
    public bool InitialTerrainGenerated { get; private set; } = false;


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
        OnChunkGenerated += CheckChunkEdges;

    }

    private void OnDestroy()
    {
        OnChunkGenerated -= CheckChunkEdges;
    }



    private void Update()
    {

        // Loop through each chunk that needs updating
        for (int i = 0; i < chunksThatNeedUpdating.Count; i++)
        {
            NeedsUpdating n = chunksThatNeedUpdating[i];
            // Add to the timer
            n.TimeSinceAdded += Time.deltaTime;

            // Chunk needs updating
            if (n.TimeSinceAdded >= ChunkWaitSecondsBeforeUpdate)
            {
                // Remove it from the list
                chunksThatNeedUpdating.Remove(n);
                // Also reset the timers for each other chunk
                for (int j = 0; j < chunksThatNeedUpdating.Count; j++)
                {
                    chunksThatNeedUpdating[j].TimeSinceAdded = 0;
                }

                // Update the chunk
                n.TerrainChunk.RecalculateMesh(MeshSettings);
                //n.TerrainChunk.RecalculateTexture(Texture_GroundSettings);

                // Call the event if something happened
                OnChunksUpdated.Invoke();


                // Break out of the loop - we don't want to process more than one chunk each tick
                break;
            }
        }


        /*
        if (IsGenerating && Time.frameCount % 30 == 0)
        {
            GC.Collect();
        }
        */



    }



    public void GenerateInitialTerrain(float viewVistanceWorld)
    {
        if (!IsGenerating)
        {
            if (DoRandomSeed)
            {
                Seed = Noise.RandomSeed;
            }

            GenerateInitialArea(Seed, viewVistanceWorld);
        }
    }


    public void Clear()
    {
        ThreadedDataRequester.Clear();

        TerrainChunkManager.Clear();
        chunksThatNeedUpdating.Clear();

        foreach (Hole h in GolfHoles)
        {
            h.Destroy();
        }
        GolfHoles.Clear();

        InitialTerrainGenerated = false;
    }



    private IEnumerator WaitUntiInitialChunksGenerated(List<Vector2Int> initialChunks, DateTime start)
    {
        while (initialChunks.Count > 0)
        {
            // Remove all chunks that have been generated
            initialChunks.RemoveAll((x) => TerrainChunkManager.TerrainChunkExists(x));

            yield return null;
        }

        OnChunksUpdated.Invoke();

        InitialTerrainGenerated = true;

        Debug.Log("Generated initial area in " + (DateTime.Now - start).TotalSeconds.ToString("0.0") + " seconds with " + GolfHoles.Count + " holes.");
    }

    private void GenerateInitialArea(int seed, float viewVistanceWorld)
    {
        DateTime before = DateTime.Now;

        InitialTerrainGenerated = false;
        IsGenerating = true;


        // Reset the whole terrain map
        TerrainChunkManager.Clear();

        // Get the chunks
        List<Vector2Int> initialChunks = GetAllPossibleNearbyChunks(ORIGIN, viewVistanceWorld);



        // Generate in that area
        foreach (Vector2Int chunk in initialChunks)
        {
            // Generate the chunk
            TryGenerateChunk(chunk, seed);
        }

        // Chunks have been updated 
        OnChunksUpdated.Invoke();

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

            ThreadedDataRequester.RequestData(() => GenerateTerrainMap(chunk, seed, chunkBounds), OnTerrainMapGenerated);
        }
    }


    private TerrainMap GenerateTerrainMap(Vector2Int chunk, int seed, in Bounds chunkBounds)
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
        float[,] bunkersRaw = Noise.Perlin(NoiseSettings_Bunker, bunkerSeed, noiseSamplePoints);

        // Holes
        int holeSeed = Noise.Seed(bunkerSeed.ToString());
        float[,] holesRaw = Noise.Perlin(NoiseSettings_Holes, holeSeed, noiseSamplePoints);

        // Create masks from the bunkers and holes
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Not a bunker here
                if (!(bunkersRaw[x, y] >= TerrainSettings_Green.BunkerNoiseThresholdMinMax.x && bunkersRaw[x, y] <= TerrainSettings_Green.BunkerNoiseThresholdMinMax.y))
                {
                    bunkersRaw[x, y] = TerrainMap.Point.Empty;
                }

                // Not a hole
                if (!(holesRaw[x, y] >= TerrainSettings_Green.HoleNoiseThresholdMinMax.x && holesRaw[x, y] <= TerrainSettings_Green.HoleNoiseThresholdMinMax.y))
                {
                    holesRaw[x, y] = TerrainMap.Point.Empty;
                }
            }
        }

        // Return the terrain map
        return new TerrainMap(chunk, width, height, localVertexPositions, chunkBounds, heightsRaw, bunkersRaw, holesRaw, TerrainSettings_Green);
    }



    private void OnTerrainMapGenerated(object terrainMapObject)
    {
        TerrainMap terrainMap = (TerrainMap)terrainMapObject;

        // Get the bunkers
        ThreadedDataRequester.RequestData(() => Hole.CalculateHoles(ref terrainMap), OnTerrainMapUpdated);
    }



    private void OnTerrainMapUpdated(object newHolesObject)
    {
        Hole.NewHoles h = (Hole.NewHoles)newHolesObject;

        // Update them all
        foreach (Hole hole in h.Holes)
        {
            hole.Flag = Instantiate(GolfHoleFlagPrefab, transform);

            hole.UpdateHole();

            GolfHoles.Add(hole);
        }

        // Create the new chunk
        TerrainChunkManager.AddNewChunk(h.TerrainMap.Chunk, h.TerrainMap.Bounds, h.TerrainMap, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer, MeshSettings, Texture_GroundSettings);

        // Invoke the event
        OnChunkGenerated.Invoke(h.TerrainMap.Chunk);
        IsGenerating = false;
    }






    private void CheckChunkEdges(Vector2Int pos)
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

        // Now add the neighbours
        ThreadedDataRequester.RequestData(() => AddNeighbours(newChunk, relativeNeighbours), CheckChunksAfterAddingNeighbours);

    }


    private List<TerrainChunk> AddNeighbours(in TerrainChunk newChunk, in List<(TerrainChunk, Vector2Int)> relativeNeighbours)
    {
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

        // Return any chunks that need updating
        return chunksUpdated;
    }



    private void CheckChunksAfterAddingNeighbours(object chunksUpdatedObject)
    {
        // Update the holes

        // Destroy any holes that need to be
        foreach (Hole h in GolfHoles)
        {
            if (h.ShouldBeDestroyed)
            {
                h.Destroy();
            }
        }

        // Remove all holes that have no vertices
        GolfHoles.RemoveWhere((x) => x.Vertices.Count == 0 || x.ShouldBeDestroyed);

        // Update any holes
        foreach (Hole h in GolfHoles)
        {
            if (h.NeedsUpdating)
            {
                h.UpdateHole();
            }
        }


        // Now let the terrain be updated

        // Get the chunks that need updating
        List<TerrainChunk> chunksUpdated = (List<TerrainChunk>)chunksUpdatedObject;

        // Add the chunks to the queue to be updated
        foreach (TerrainChunk c in chunksUpdated)
        {
            // First check if this chunk has already been added
            NeedsUpdating n = chunksThatNeedUpdating.Find((x) => x.TerrainChunk.Position.Equals(c.Position));
            // Create a new object and assign the chunk if not
            if (n == null)
            {
                n = new NeedsUpdating
                {
                    TerrainChunk = c
                };
                // Add it if it does not exist
                chunksThatNeedUpdating.Add(n);
            }

            // Reset the timer
            n.TimeSinceAdded = 0;
        }
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




    private Vector2[,] ConvertWorldPointsToPerlinSample(in Vector3[,] points)
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




    public static Vector3 CalculateDistanceBetweenVertices(in Bounds b, int divisions)
    {
        return (b.max - b.min) / divisions;
    }





    public static Vector3[,] CalculateVertexPointsForChunk(in Bounds chunk, in TerrainSettings settings)
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




    private class NeedsUpdating
    {
        public TerrainChunk TerrainChunk;
        public float TimeSinceAdded;
    }

}
