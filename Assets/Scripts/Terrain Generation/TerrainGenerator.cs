using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
    private Dictionary<Vector2Int, ChunkData> Chunks = new Dictionary<Vector2Int, ChunkData>();
    public TerrainChunkManager TerrainChunkManager;
    public WorldObjectGenerator WorldObjectGenerator;

    public Transform HolesWorldObjectParent;
    private HashSet<Hole> GolfHoles = new HashSet<Hole>();

    private List<NeedsUpdating> chunksThatNeedUpdating = new List<NeedsUpdating>();
    public const float ChunkWaitSecondsBeforeUpdate = 0.5f;

    public UnityAction OnInitialTerrainGenerated;
    private UnityAction<ChunkData> OnChunkGenerated;
    public UnityAction<HashSet<Vector2Int>> OnChunksUpdated;


    public bool IsGenerating { get; private set; } = false;
    public bool InitialTerrainGenerated { get; private set; } = false;


    [Header("Settings")]
    public MeshSettings MeshSettings;
    [Space]
    public NoiseSettings NoiseSettings_Green;
    public NoiseSettings NoiseSettings_Bunker;
    public NoiseSettings NoiseSettings_Holes;
    public NoiseSettings NoiseSettings_Trees;
    public NoiseSettings NoiseSettings_Rocks;
    [Space]
    public TerrainSettings TerrainSettings_Green;

    [Space]
    public TextureSettings Texture_GroundSettings;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;


    private CancellationTokenSource TaskCancelToken = new CancellationTokenSource();



    private void Awake()
    {
        OnChunkGenerated += CheckChunkEdges;
    }

    private void OnDestroy()
    {
        OnChunkGenerated -= CheckChunkEdges;
    }


    public void Clear()
    {
        TaskCancelToken.Cancel();
        TaskCancelToken = new CancellationTokenSource();

        Chunks.Clear();
        chunksThatNeedUpdating.Clear();

        foreach (Hole h in GolfHoles)
        {
            h.Destroy();
        }
        GolfHoles.Clear();

        InitialTerrainGenerated = false;
    }


    public TerrainData TerrainData
    {
        get
        {
            List<TerrainChunkData> chunks = new List<TerrainChunkData>();
            // Add each TerrainMapData
            foreach (ChunkData m in Chunks.Values)
            {
                chunks.Add(m.Data);
            }

            // Create the object and set the data
            TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
            terrain.SetData(Seed, chunks, GetHoleData());

            return terrain;
        }
    }

    public List<HoleData> GetHoleData()
    {
        List<HoleData> holes = new List<HoleData>();
        foreach (Hole h in GolfHoles)
        {
            holes.Add(new HoleData(h.Centre));
        }
        return holes;

    }

    public bool GetTerrainChunkData(Vector2Int chunk, out TerrainChunkData data)
    {
        data = default;

        bool val = Chunks.TryGetValue(chunk, out ChunkData d);
        if (val)
        {
            data = d.Data;
        }

        return val;
    }


    public HashSet<TerrainChunkData> GetChunkData(IEnumerable<Vector2Int> chunks)
    {
        HashSet<TerrainChunkData> all = new HashSet<TerrainChunkData>();

        foreach (Vector2Int pos in chunks)
        {
            if (GetTerrainChunkData(pos, out TerrainChunkData d))
            {
                all.Add(d);
            }
        }

        return all;
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

                // Update the chunk's mesh
                MeshGenerator.UpdateMeshData(ref n.Data.MeshData, n.Data.TerrainMap);
                n.Data.MeshData.UpdateMesh(ref n.Data.Data.MainMesh, MeshSettings);

                //n.Data.Data.BiomeColourMap = TextureGenerator.GenerateBiomeColourMap(n.Data.TerrainMap, Texture_GroundSettings);

                // Call the event if something happened
                OnChunksUpdated.Invoke(new HashSet<Vector2Int>() { n.Data.TerrainMap.Chunk });


                // Break out of the loop - we don't want to process more than one chunk each tick
                break;
            }
        }
    }







    private TerrainMap.Point GetLowestPoint(IEnumerable<TerrainMap.Point> points)
    {
        if (points != null)
        {
            // Get the point with the lowest Y value
            TerrainMap.Point lowest = points.FirstOrDefault();
            foreach (TerrainMap.Point p in points)
            {
                if (p.LocalVertexPosition.y < lowest.LocalVertexPosition.y)
                {
                    lowest = p;
                }
            }

            return lowest;
        }
        return default;
    }




    public void GenerateInitialTerrain(IEnumerable<Vector2Int> chunks)
    {
        DateTime before = DateTime.Now;

        Clear();

        // Get random seed
        if (DoRandomSeed)
        {
            Seed = Noise.RandomSeed;
        }

        InitialTerrainGenerated = false;
        IsGenerating = true;


        // Generate the initial chunks
        TryGenerateChunks(chunks, Seed);

        // Wait until the chunks have been generated
        StartCoroutine(WaitUntiInitialChunksGenerated(new List<Vector2Int>(chunks), before));
    }



    private IEnumerator WaitUntiInitialChunksGenerated(List<Vector2Int> initialChunks, DateTime start)
    {
        int totalChunks = initialChunks.Count;
        while (initialChunks.Count > 0)
        {
            // Remove all chunks that have been generated
            initialChunks.RemoveAll((x) => Chunks.ContainsKey(x));

            yield return null;
        }



        InitialTerrainGenerated = true;
        OnInitialTerrainGenerated.Invoke();


        string message = "* Generated initial area in " + (DateTime.Now - start).TotalSeconds.ToString("0.0") + " seconds with " + totalChunks + " chunks and " + GolfHoles.Count + " holes.";

        Debug.Log(message);
    }



    private void TryGenerateChunk(Vector2Int chunk, int seed)
    {
        if (!Chunks.ContainsKey(chunk))
        {
            DateTime before = DateTime.Now;

            IsGenerating = true;

            // Get the chunk bounds
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            // Generate the TerrainMap
            Task<TerrainMap> generateMap = Task<TerrainMap>.Factory.StartNew(() => GenerateTerrainMap(chunk, seed, chunkBounds), TaskCancelToken.Token);
            TerrainMap map = generateMap.Result;



            // Continue and get the holes
            Task<HashSet<Hole>> newHoles = generateMap.ContinueWith((m) => Hole.CalculateHoles(ref map), TaskCancelToken.Token);

            // Update them all
            foreach (Hole hole in newHoles.Result)
            {
                hole.UpdateHole();

                GolfHoles.Add(hole);
            }




            List<WorldObjectData> data = WorldObjectGenerator.CalculateDataForChunk(map);
           


            // Assign the biomes
            Biome.Type[,] biomes = new Biome.Type[map.Width, map.Height];
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    biomes[x, y] = map.Points[x, y].Biome;
                }
            }



            Texture2D colourMap = TextureGenerator.GenerateBiomeColourMap(map, Texture_GroundSettings);

            MeshGenerator.MeshData meshData = null;
            MeshGenerator.UpdateMeshData(ref meshData, map);

            Mesh mesh = null;
            meshData.UpdateMesh(ref mesh, MeshSettings);

            TerrainChunkData chunkData = new TerrainChunkData(map.Chunk.x, map.Chunk.y, map.Bounds.center, map.Bounds.size, biomes, colourMap, mesh, data);


            ChunkData d = new ChunkData() { Data = chunkData, TerrainMap = map, MeshData = meshData };

            // Create the new chunk
            Chunks.Add(chunk, d);

            // Invoke the event
            OnChunkGenerated.Invoke(d);
            IsGenerating = false;

            //Debug.Log("Took " + (DateTime.Now - before).Milliseconds + " to generate chunk (Map: " + (beforeHoles - before).Milliseconds + ", Holes" + (beforeHoles - before).Milliseconds + ")");
        }
    }


    public void TryGenerateChunks(IEnumerable<Vector2Int> chunks, int seed)
    {
        // Try and generate them
        foreach (Vector2Int chunk in chunks)
        {
            // Generate only if it does not already exist
            if (!Chunks.TryGetValue(chunk, out ChunkData _))
            {
                TryGenerateChunk(chunk, seed);
            }
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
        float[,] bunkerFloatMask = Noise.Perlin(NoiseSettings_Bunker, bunkerSeed, noiseSamplePoints);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Not a bunker here
                if (!(bunkerFloatMask[x, y] >= TerrainSettings_Green.BunkerNoiseThresholdMinMax.x && bunkerFloatMask[x, y] <= TerrainSettings_Green.BunkerNoiseThresholdMinMax.y))
                {
                    bunkerFloatMask[x, y] = TerrainMap.Point.Empty;
                }
            }
        }


        // Masks

        // Hole
        int holeSeed = Noise.Seed(bunkerSeed.ToString());
        bool[,] holeMask = Noise.PerlinMask(NoiseSettings_Holes, holeSeed, TerrainSettings_Green.HoleNoiseThresholdMinMax, noiseSamplePoints);


        // Trees
        int treeSeed = Noise.Seed(holeSeed.ToString());
        bool[,] treeMask = Noise.PerlinMask(NoiseSettings_Trees, treeSeed, TerrainSettings_Green.Trees.NoiseThresholdMinMax, noiseSamplePoints);

        // Rocks
        int rockSeed = Noise.Seed(treeSeed.ToString());
        bool[,] rockMask = Noise.PerlinMask(NoiseSettings_Rocks, rockSeed, TerrainSettings_Green.Rocks.NoiseThresholdMinMax, noiseSamplePoints);



        float[,] heightsBeforeHole = CalculateHeightsBeforeHole(width, height, TerrainSettings_Green, heightsRaw, bunkerFloatMask, holeMask,
            treeMask, rockMask, out Biome.Type[,] biomes, out Biome.Decoration[,] decoration);



        // Return the terrain map
        return new TerrainMap(chunk, width, height, localVertexPositions, chunkBounds, heightsBeforeHole, holeMask, biomes, decoration);
    }




    private void CheckChunkEdges(ChunkData data)
    {
        HashSet<(TerrainMap, Vector2Int)> relativeNeighbours = new HashSet<(TerrainMap, Vector2Int)>();

        // Check the 3x3 of nearby chunks
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector2Int relativePos = new Vector2Int(x, y);
                Vector2Int neighbour = data.TerrainMap.Chunk + relativePos;
                // Don't add its self
                if (!(neighbour.x == data.TerrainMap.Chunk.x && neighbour.y == data.TerrainMap.Chunk.y))
                {
                    // Get the neighbour chunk
                    if (Chunks.TryGetValue(neighbour, out ChunkData d))
                    {
                        relativeNeighbours.Add((d.TerrainMap, relativePos));
                    }
                }
            }
        }

        // Now add the neighbours
        Task<HashSet<TerrainMap>> addNeighboursTask = Task<HashSet<TerrainMap>>.Factory.StartNew(
            () => AddNeighbours(data.TerrainMap, relativeNeighbours), TaskCancelToken.Token);




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

        // Add the chunks to the queue to be updated
        foreach (TerrainMap m in addNeighboursTask.Result)
        {
            // First check if this chunk has already been added
            NeedsUpdating n = chunksThatNeedUpdating.Find((x) => x.Data.TerrainMap.Chunk.Equals(m.Chunk));
            // Create a new object and assign the chunk if not
            if (n == null)
            {
                if (Chunks.TryGetValue(m.Chunk, out ChunkData chunkData))
                {
                    n = new NeedsUpdating
                    {
                        Data = chunkData
                    };
                    // Add it if it does not exist
                    chunksThatNeedUpdating.Add(n);
                }
            }

            // Reset the timer
            n.TimeSinceAdded = 0;
        }
    }


    private HashSet<TerrainMap> AddNeighbours(in TerrainMap newChunk, in HashSet<(TerrainMap, Vector2Int)> relativeNeighbours)
    {
        // Record which chunks have changed
        HashSet<TerrainMap> chunksUpdated = new HashSet<TerrainMap>();

        // Update the edge references for both the new and existing chunk
        foreach ((TerrainMap, Vector2Int) chunk in relativeNeighbours)
        {
            // New chunk
            newChunk.AddEdgeNeighbours(chunk.Item2.x, chunk.Item2.y, chunk.Item1, out bool needsUpdateA);

            // Existing chunk
            chunk.Item1.AddEdgeNeighbours(-chunk.Item2.x, -chunk.Item2.y, newChunk, out bool needsUpdateB);

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





    public HashSet<Vector2Int> GetAllPossibleNearbyChunks(Vector3 position, int chunks)
    {
        // Calculate the centre chunk
        Vector2Int centre = TerrainChunkManager.WorldToChunk(position);

        HashSet<Vector2Int> nearbyChunks = new HashSet<Vector2Int>();

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


    public HashSet<Vector2Int> GetAllPossibleNearbyChunks(Vector3 position, float radius)
    {
        return GetAllPossibleNearbyChunks(position, Mathf.RoundToInt(radius / TerrainChunkManager.ChunkSizeWorldUnits));
    }



    public HashSet<Vector2Int> GetNearbyChunksToGenerate(Vector3 position, int chunks)
    {
        // Calculate the centre chunk
        Vector2Int centre = TerrainChunkManager.WorldToChunk(position);
        HashSet<Vector2Int> nearbyChunks = new HashSet<Vector2Int>();

        // Generate in that area
        for (int y = -chunks; y <= chunks; y++)
        {
            for (int x = -chunks; x <= chunks; x++)
            {
                Vector2Int chunk = new Vector2Int(centre.x + x, centre.y + y);
                if (!Chunks.ContainsKey(chunk))
                {
                    nearbyChunks.Add(chunk);
                }
            }
        }

        return nearbyChunks;
    }

    public HashSet<Vector2Int> GetNearbyChunksToGenerate(Vector3 position, float radius)
    {
        return GetNearbyChunksToGenerate(position, Mathf.RoundToInt(radius / TerrainChunkManager.ChunkSizeWorldUnits));
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

















    private float[,] CalculateHeightsBeforeHole(int width, int height, in TerrainSettings settings, in float[,] rawHeights, in float[,] bunkerHeights,
        bool[,] holeMask, bool[,] treeMask, bool[,] rockMask, out Biome.Type[,] biomes, out Biome.Decoration[,] decoration)
    {
        settings.ValidateValues();

        // Assign memory for it all
        float[,] heights = new float[width, height];
        biomes = new Biome.Type[width, height];
        decoration = new Biome.Decoration[width, height];
        // Get a new animation curve to use in the thread
        AnimationCurve threadSafe = new AnimationCurve(settings.HeightDistribution.keys);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Height stuff
                float finalHeight = rawHeights[x, y];

                // Apply curve 
                if (settings.UseCurve)
                {
                    finalHeight = threadSafe.Evaluate(rawHeights[x, y]);
                }

                finalHeight *= settings.HeightMultiplier;

                // Take away the bunker if we need to
                if (settings.DoBunkers)
                {
                    finalHeight -= (bunkerHeights[x, y] * settings.BunkerMultiplier);
                }

                heights[x, y] = finalHeight;



                // Biome stuff
                Biome.Type biome = settings.MainBiome;

                // Do a bunker
                if (settings.DoBunkers && !Mathf.Approximately(bunkerHeights[x, y], TerrainMap.Point.Empty))
                {
                    biome = settings.BunkerBiome;
                }

                // Hole is more important
                if (holeMask[x, y])
                {
                    biome = settings.HoleBiome;
                }

                biomes[x, y] = biome;




                // Decoration stuff
                Biome.Decoration decor = Biome.Decoration.None;

                // Do a rock
                if (settings.Rocks.DoObject && rockMask[x, y])
                {
                    decor = Biome.Decoration.Rock;
                }

                // Tree is more important 
                if (settings.Trees.DoObject && treeMask[x, y])
                {
                    decor = Biome.Decoration.Tree;
                }


                // Don't allow decoration to exist in these biomes
                if(biomes[x,y] == settings.HoleBiome || biomes[x,y] == settings.BunkerBiome)
                {
                    decor = Biome.Decoration.None;
                }

                decoration[x, y] = decor;
            }
        }


        return heights;
    }











    private struct ChunkData
    {
        public TerrainChunkData Data;
        public TerrainMap TerrainMap;
        public MeshGenerator.MeshData MeshData;
    }


    private class NeedsUpdating
    {
        public ChunkData Data;
        public float TimeSinceAdded;
    }

}
