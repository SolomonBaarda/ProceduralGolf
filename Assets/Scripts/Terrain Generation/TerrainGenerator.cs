using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
    private Dictionary<Vector2Int, ChunkData> Chunks = new Dictionary<Vector2Int, ChunkData>();
    public TerrainChunkManager TerrainChunkManager;
    public WorldObjectGenerator WorldObjectGenerator;

    public Transform HolesWorldObjectParent;

    public bool IsGenerating { get; private set; } = false;
    public bool InitialTerrainGenerated { get; private set; } = false;


    [Header("Settings")]
    public MeshSettings MeshSettings;
    [Space]
    public TerrainSettings Settings;

    [Space]
    public TextureSettings TextureSettings;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;

    private UnityEvent OnAbortThreads = new UnityEvent();

    private delegate void Pass();


    public void Clear()
    {
        Chunks.Clear();

        InitialTerrainGenerated = false;
    }


    private TerrainData GenerateTerrainData()
    {
        List<TerrainChunkData> chunks = new List<TerrainChunkData>();
        // Add each TerrainMapData
        foreach (ChunkData m in Chunks.Values)
        {
            chunks.Add(m.Data);
        }

        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(Seed, chunks, new List<HoleData>(), Settings.name);

        return terrain;
    }








    public void Generate(List<Vector2Int> chunks, GameManager.LoadLevel callback)
    {
        Clear();

        // Get random seed
        if (DoRandomSeed)
        {
            Seed = Noise.RandomSeed;
        }

        InitialTerrainGenerated = false;
        IsGenerating = true;

        StartCoroutine(WaitForGenerate(chunks, Seed, callback));
    }

    private IEnumerator WaitForGenerate(List<Vector2Int> chunks, int seed, GameManager.LoadLevel callback)
    {
        DateTime before = DateTime.Now;
        DateTime last = before;

        List<Thread> threads = new List<Thread>();
        bool aborted = false;
        UnityAction a = () =>
        {
            foreach (Thread t in threads)
            {
                t.Abort();
            }
            IsGenerating = false;
            aborted = true;
        };

        OnAbortThreads.AddListener(a);

        Dictionary<Vector2Int, (TerrainMap, List<WorldObjectData>)> maps = new Dictionary<Vector2Int, (TerrainMap, List<WorldObjectData>)>();

        // FIRST PASS
        // Generate the initial chunks

        foreach (Vector2Int chunk in chunks)
        {
            // Generate only if it does not already exist
            if (!Chunks.TryGetValue(chunk, out ChunkData _))
            {
                // Get the chunk bounds
                // This has to be called from the main thread
                Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

                Thread t = new Thread(
                    () =>
                    {
                        GenerateChunk(chunk, seed, chunkBounds, out TerrainMap m, out List<WorldObjectData> worldObjects);

                        lock (maps)
                        {
                            maps.Add(chunk, (m, worldObjects));
                        }
                    }
                    );
                threads.Add(t);

                t.Start();
            }
        }

        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }

        Debug.Log("* First pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
            yield break;
        last = DateTime.Now;


        // SECOND PASS

        //

        // Normalisation

        // Do holes

        Debug.Log("* Second pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
            yield break;
        last = DateTime.Now;


        // THIRD PASS

        // Construct textures and meshes
        // This needs to be done in the main thread
        foreach (KeyValuePair<Vector2Int, (TerrainMap, List<WorldObjectData>)> pair in maps)
        {
            TerrainMap map = pair.Value.Item1;
            Texture2D colourMap = TextureGenerator.GenerateBiomeColourMap(map, TextureSettings);

            MeshGenerator.MeshData meshData = null;
            MeshGenerator.UpdateMeshData(ref meshData, map);

            Mesh mesh = null;
            meshData.UpdateMesh(ref mesh, MeshSettings);

            TerrainChunkData chunkData = new TerrainChunkData(map.Chunk.x, map.Chunk.y, map.Bounds.center, map.Biomes, colourMap, mesh, pair.Value.Item2);


            ChunkData d = new ChunkData() { Data = chunkData, TerrainMap = map, MeshData = meshData };

            // Create the new chunk
            Chunks.Add(pair.Key, d);
        }




        // Instantiate trees etc



        // FINISHED GENERATING
        Debug.Log("* Third pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
            yield break;
        last = DateTime.Now;

        Debug.Log("* Generated terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");

        OnAbortThreads.RemoveListener(a);

        IsGenerating = false;
        callback(GenerateTerrainData());
    }



    private void GenerateChunk(Vector2Int chunk, int seed, Bounds chunkBounds, out TerrainMap map, out List<WorldObjectData> worldObjects)
    {
        // Generate the TerrainMap
        map = GenerateTerrainMap(chunk, seed, chunkBounds);

        // Continue and get the holes
        //Task<HashSet<FloodFillBiome>> newHoles = generateMap.ContinueWith((m) => FloodFillBiome.CalculatePoints(ref map, Settings.HoleBiome), TaskCancelToken.Token);

        // Continue and get the lakes
        //Task<HashSet<FloodFillBiome>> newLakesTask = newHoles.ContinueWith((m) => FloodFillBiome.CalculatePoints(ref map, Current.Bunker.Biome), TaskCancelToken.Token);
        //HashSet<FloodFillBiome> l = newLakesTask.Result;



        // Update them all
        /*
        foreach (FloodFillBiome hole in newHoles.Result)
        {
            hole.Update();
            hole.SetAllVerticesConnectedToThis();

            GolfHoles.Add(hole);
        }
        */



        // Get the positions of the world objects
        worldObjects = WorldObjectGenerator.CalculateDataForChunk(map);
    }









    private TerrainMap GenerateTerrainMap(Vector2Int chunk, int seed, in Bounds chunkBounds)
    {
        // Get the vertex points
        Vector3[,] vertices = CalculateVertexPointsForChunk(chunkBounds, Settings);
        Vector3[,] localVertexPositions = CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
        Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

        int width = vertices.GetLength(0), height = vertices.GetLength(1);

        // Heights
        float[,] heightsRaw = Noise.Perlin(Settings.NoiseMain, seed, noiseSamplePoints);


        // Masks

        // Hole
        int holeSeed = Noise.Seed(seed.ToString());
        bool[,] holeMask = Noise.PerlinMask(Settings.NoiseHole, holeSeed, Settings.HoleNoiseThresholdMinMax, noiseSamplePoints);

        // Bunkers
        int bunkerSeed = Noise.Seed(holeSeed.ToString());
        float[,] bunkerFloatMask = Noise.Perlin(Settings.NoiseBunker, bunkerSeed, noiseSamplePoints);

        // Lakes
        int lakeSeed = Noise.Seed(bunkerSeed.ToString());
        float[,] lakeFloatMask = Noise.Perlin(Settings.NoiseLake, lakeSeed, noiseSamplePoints);

        // Apply the masks here
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Not a bunker here
                if (!(bunkerFloatMask[x, y] >= Settings.Bunker.NoiseThresholdMinMax.x && bunkerFloatMask[x, y] <= Settings.Bunker.NoiseThresholdMinMax.y))
                {
                    bunkerFloatMask[x, y] = 0;
                }
                // Not a lake here
                if (!(lakeFloatMask[x, y] >= Settings.Lake.NoiseThresholdMinMax.x && lakeFloatMask[x, y] <= Settings.Lake.NoiseThresholdMinMax.y))
                {
                    lakeFloatMask[x, y] = 0;
                }
            }
        }





        // Trees
        int treeSeed = Noise.Seed(lakeSeed.ToString());
        bool[,] treeMask = Noise.PerlinMask(Settings.NoiseTree, treeSeed, Settings.Trees.NoiseThresholdMinMax, noiseSamplePoints);

        // Rocks
        int rockSeed = Noise.Seed(treeSeed.ToString());
        bool[,] rockMask = Noise.PerlinMask(Settings.NoiseRock, rockSeed, Settings.Rocks.NoiseThresholdMinMax, noiseSamplePoints);



        float[,] heightsBeforeHole = CalculateHeightsBeforeHole(width, height, Settings, heightsRaw, bunkerFloatMask, lakeFloatMask, holeMask,
            treeMask, rockMask, out Biome.Type[,] biomes, out List<Biome.Decoration>[,] decoration);



        // Return the terrain map
        return new TerrainMap(chunk, width, height, localVertexPositions, chunkBounds, heightsBeforeHole, biomes, decoration);
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

















    private float[,] CalculateHeightsBeforeHole(int width, int height, in TerrainSettings settings, in float[,] rawHeights, in float[,] bunkerHeights, in float[,] lakeHeights,
        bool[,] holeMask, bool[,] treeMask, bool[,] rockMask, out Biome.Type[,] biomes, out List<Biome.Decoration>[,] decoration)
    {
        settings.ValidateValues();

        // Assign memory for it all
        float[,] heights = new float[width, height];
        biomes = new Biome.Type[width, height];
        decoration = new List<Biome.Decoration>[width, height];
        // Get a new animation curve to use in the thread
        AnimationCurve threadSafe = new AnimationCurve(settings.HeightDistribution.keys);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate the biome
                decoration[x, y] = CalculateDecorationBeforeBiome(settings, treeMask[x, y], rockMask[x, y]);
                biomes[x, y] = CalculateBiomeAndEditDecoration(settings, holeMask[x, y], bunkerHeights[x, y], lakeHeights[x, y], ref decoration[x, y]);

                // Calculate the height
                heights[x, y] = CalculateHeight(settings, rawHeights[x, y], threadSafe, bunkerHeights[x, y], lakeHeights[x, y]);
            }
        }


        return heights;
    }


    private float CalculateHeight(TerrainSettings settings, float rawHeight, AnimationCurve threadSafe, float bunkerHeight, float lakeHeight)
    {
        float height = rawHeight * settings.HeightMultiplier;

        // Apply curve 
        if (settings.UseCurve)
        {
            height = threadSafe.Evaluate(rawHeight) * settings.HeightMultiplier;
        }


        // Take away the lake if we need to
        if (settings.Lake.Do && !Mathf.Approximately(lakeHeight, 0))
        {
            height -= (lakeHeight * settings.Lake.Multiplier);
        }
        // Only do the bunker if the lake fails
        else if (settings.Bunker.Do)
        {
            height -= (bunkerHeight * settings.Bunker.Multiplier);
        }



        return height;
    }


    private List<Biome.Decoration> CalculateDecorationBeforeBiome(TerrainSettings settings, bool treeMask, bool rockMask)
    {
        List<Biome.Decoration> decoration = new List<Biome.Decoration>();

        // Do tree
        if (settings.Trees.DoObject && treeMask)
        {
            decoration.Add(Biome.Decoration.Tree);
        }
        // Do rock
        if (settings.Rocks.DoObject && rockMask)
        {
            decoration.Add(Biome.Decoration.Rock);
        }

        return decoration;
    }


    private Biome.Type CalculateBiomeAndEditDecoration(TerrainSettings settings, bool isHole, float bunkerHeight, float lakeHeight, ref List<Biome.Decoration> decoration)
    {
        // Hole has first priority
        if (isHole)
        {
            decoration.Clear();
            return settings.HoleBiome;
        }

        // Lake should be here
        if (settings.Lake.Do && !Mathf.Approximately(lakeHeight, 0))
        {
            decoration.Clear();
            return settings.Lake.Biome;
        }

        // Next is bunker
        if (settings.Bunker.Do && !Mathf.Approximately(bunkerHeight, 0))
        {
            decoration.Clear();
            return settings.Bunker.Biome;
        }


        // Set forest biome
        if (decoration.Count > 0)
        {
            if (decoration.Contains(Biome.Decoration.Tree))
            {
                return settings.Trees.DesiredBiome;
            }
            if (decoration.Contains(Biome.Decoration.Rock))
            {
                return settings.Rocks.DesiredBiome;
            }
        }


        // Set main biome
        return settings.MainBiome;
    }







    private struct ChunkData
    {
        public TerrainChunkData Data;
        public TerrainMap TerrainMap;
        public MeshGenerator.MeshData MeshData;
    }


}
