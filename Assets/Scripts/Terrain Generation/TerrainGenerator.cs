using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
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
        InitialTerrainGenerated = false;
        OnAbortThreads.Invoke();
    }

    private void OnDestroy()
    {
        Clear();
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
        void a()
        {
            foreach (Thread t in threads)
            {
                t.Abort();
            }
            IsGenerating = false;
            aborted = true;
        }

        OnAbortThreads.AddListener(a);

        Dictionary<Vector2Int, TerrainMap> maps = new Dictionary<Vector2Int, TerrainMap>();
        int width = Settings.SamplePointFrequency, height = Settings.SamplePointFrequency;
        Vector3[] localVertexPositions = CalculateLocalVertexPointsForChunk(new Vector3(TerrainChunkManager.ChunkGrid.cellSize.x, 0, TerrainChunkManager.ChunkGrid.cellSize.y), Settings.SamplePointFrequency);

        // FIRST PASS
        // Generate the initial chunks
        float min = float.MaxValue, max = float.MinValue, minLake = float.MaxValue, maxLake = float.MinValue, minBunker = float.MaxValue, maxBunker = float.MinValue;
        object threadLock = new object();

        foreach (Vector2Int chunk in chunks)
        {
            // Get the chunk bounds
            // This has to be called from the main thread
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            Thread t = new Thread(
                () =>
                {
                    GenerateTerrainMapRawData(chunk, seed, chunkBounds, width, height, in localVertexPositions, out TerrainMap map);

                    // Gain access to the critical region once we have calculated the data
                    lock (threadLock)
                    {
                        maps.Add(chunk, map);

                        // Heights min max
                        if (map.HeightsMin < min)
                            min = map.HeightsMin;
                        if (map.HeightsMax > max)
                            max = map.HeightsMax;

                        // Lake min max
                        if (map.LakeHeightMin < minLake)
                            minLake = map.LakeHeightMin;
                        if (map.LakeHeightMax > maxLake)
                            maxLake = map.LakeHeightMax;

                        // Bunker min max
                        if (map.BunkerHeightMin < minBunker)
                            minBunker = map.BunkerHeightMin;
                        if (map.BunkerHeightMax > maxBunker)
                            maxBunker = map.BunkerHeightMax;
                    }
                }
                )
            { Name = "Pass 1 (" + chunk.x + "," + chunk.y + ")" };
            lock (threads)
            {
                threads.Add(t);
                t.Start();
            }
        }

        // Wait for threads to complete
        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }

        Debug.Log("* First pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
        {
            OnAbortThreads.RemoveListener(a);
            IsGenerating = false;
            Clear();
            yield break;
        }
        last = DateTime.Now;


        // SECOND PASS
        Dictionary<Vector2Int, MeshGenerator.MeshData> meshData = new Dictionary<Vector2Int, MeshGenerator.MeshData>();

        threads.Clear();
        foreach (TerrainMap map in maps.Values)
        {
            // Use height curve to calculate new height distribution
            AnimationCurve threadSafe = new AnimationCurve(Settings.HeightDistribution.keys);

            Thread t = new Thread(
                () =>
                {
                    // Normalise heights
                    map.Normalise(min, max, minLake, maxLake, minBunker, maxBunker);
                    map.Biomes = new Biome.Type[width * height];
                    map.Decoration = new Biome.Decoration[width * height];

                    // Now calculate the actual heights from the noise
                    for (int i = 0; i < map.Heights.Length; i++)
                    {
                        map.Biomes[i] = Settings.MainBiome;

                        // Set it to hole
                        if (map.Heights[i] < 0.1)
                        {
                            map.Heights[i] = 0.1f;
                            map.Biomes[i] = Settings.HoleBiome;
                        }

                        if (Settings.UseCurve)
                        {
                            map.Heights[i] = threadSafe.Evaluate(map.Heights[i]);
                        }

                        map.Heights[i] *= Settings.HeightMultiplier;

                        // Lake has first priority
                        if (Settings.Lake.Do && map.LakeHeights[i] < 0.1f)
                        {
                            //map.Heights[i] -= (map.LakeHeights[i] * Settings.Lake.Multiplier);
                            //map.Biomes[i] = Settings.Lake.Biome;
                        }
                        // Then bunker
                        else if (Settings.Bunker.Do && map.BunkerHeights[i] < 0.2f)
                        {
                            //map.Heights[i] -= (map.BunkerHeights[i] * Settings.Bunker.Multiplier);
                            //map.Biomes[i] = Settings.Bunker.Biome;
                        }
                        // Then other land
                        else
                        {
                            // Do holes next
                            //List<FloodFillBiome> holes = new List<FloodFillBiome>();

                            // Should do decoration here 
                            // TODO
                            // out List<WorldObjectData> worldObjects

                            // Move sampling into here
                        }
                    }

                    // Now Calculate the mesh data for the chunk
                    MeshGenerator.MeshData data = null;
                    MeshGenerator.UpdateMeshData(ref data, map, localVertexPositions);

                    lock (threadLock)
                    {
                        meshData.Add(map.Chunk, data);
                    }
                }
                )
            { Name = "Pass 2 (" + map.Chunk.x + "," + map.Chunk.y + ")" }; ;
            lock (threads)
            {
                threads.Add(t);
                t.Start();
            }
        }

        // Wait for threads to complete
        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }


        Debug.Log("* Second pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
        {
            OnAbortThreads.RemoveListener(a);
            IsGenerating = false;
            Clear();
            yield break;
        }

        last = DateTime.Now;


        // THIRD PASS

        // Construct textures and meshes
        // This needs to be done in the main thread

        List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();

        foreach (TerrainMap map in maps.Values)
        {
            // Generate the mesh
            meshData.TryGetValue(map.Chunk, out MeshGenerator.MeshData data);
            Mesh mesh = null;
            data.UpdateMesh(ref mesh, MeshSettings);
            yield return null;

            // Optimise it and generate the texture
            data.Optimise(ref mesh);
            Texture2D colourMap = TextureGenerator.GenerateBiomeColourMap(map, TextureSettings);
            terrainChunks.Add(new TerrainChunkData(map.Chunk.x, map.Chunk.y, map.Bounds.center, map.Biomes, width, height, colourMap, mesh, new List<WorldObjectData>()));
            yield return null;
        }

        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(Seed, terrainChunks, new List<HoleData>(), Settings.name);




        // FINISHED GENERATING
        Debug.Log("* Third pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
        {
            OnAbortThreads.RemoveListener(a);
            IsGenerating = false;
            Clear();
            yield break;
        }
        last = DateTime.Now;

        Debug.Log("* Generated terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");

        OnAbortThreads.RemoveListener(a);
        IsGenerating = false;

        // Callback when done
        callback(terrain);
    }

    private void GenerateTerrainMapRawData(Vector2Int chunk, int seed, Bounds chunkBounds, int width, int height, in Vector3[] localVertexPositions, out TerrainMap map)
    {
        map = new TerrainMap(chunk, width, height, chunkBounds);

        // Heights
        map.Heights = Noise.GetNoise(Settings.NoiseMain, seed, chunkBounds.min, in localVertexPositions, width, height, out map.HeightsMin, out map.HeightsMax);

        // Bunkers
        int bunkerSeed = seed.ToString().GetHashCode();
        map.BunkerHeights = Noise.GetNoise(Settings.NoiseBunker, bunkerSeed, chunkBounds.min, in localVertexPositions, width, height, out map.BunkerHeightMin, out map.BunkerHeightMax);

        // Lakes
        int lakeSeed = bunkerSeed.ToString().GetHashCode();
        map.LakeHeights = Noise.GetNoise(Settings.NoiseLake, lakeSeed, chunkBounds.min, in localVertexPositions, width, height, out map.LakeHeightMin, out map.LakeHeightMax);

        // Tree mask
        int treeSeed = lakeSeed.ToString().GetHashCode();
        map.TreeMask = Noise.GetPerlinMask(Settings.NoiseTree, treeSeed, chunkBounds.min, in localVertexPositions, width, height, Settings.Trees.NoiseThresholdMinMax, out float _, out float _);

        // Rock mask
        int rockSeed = treeSeed.ToString().GetHashCode();
        map.RockMask = Noise.GetPerlinMask(Settings.NoiseRock, rockSeed, chunkBounds.min, in localVertexPositions, width, height, Settings.Rocks.NoiseThresholdMinMax, out float _, out float _);
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








    public Vector3[] CalculateLocalVertexPointsForChunk(Vector3 size, int numSamplePoints)
    {
        Vector3[] localPositions = new Vector3[numSamplePoints * numSamplePoints];
        Vector3 one = size / (numSamplePoints - 1);

        for (int y = 0; y < numSamplePoints; y++)
        {
            for (int x = 0; x < numSamplePoints; x++)
            {
                int index = y * numSamplePoints + x;
                localPositions[index] = new Vector3(one.x * x, 0, one.z * y);
            }
        }

        return localPositions;
    }






}
