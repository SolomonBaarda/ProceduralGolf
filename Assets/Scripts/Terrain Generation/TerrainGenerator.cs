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
        float min = float.MaxValue, max = float.MinValue;
        object threadLock = new object();

        foreach (Vector2Int chunk in chunks)
        {
            // Get the chunk bounds
            // This has to be called from the main thread
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            Thread t = new Thread(
                () =>
                {
                    GenerateTerrainMapRawData(chunk, seed, chunkBounds, width, height, in localVertexPositions, out TerrainMap map, out float newMin, out float newMax);
                    
                    // Gain access to the critical region once we have calculated the data
                    lock (threadLock)
                    {
                        maps.Add(chunk, map);

                        //Noise.GetMinMax(d.Heights, out float newMin, out float newMax);

                        if (newMin < min)
                        {
                            min = newMin;
                        }
                        if (newMax > max)
                        {
                            max = newMax;
                        }
                    }
                }
                );
            threads.Add(t);
            t.Start();
        }

        // Wait for threads to complete
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

        threads.Clear();
        foreach (TerrainMap map in maps.Values)
        {
            // Use height curve to calculate new height distribution
            AnimationCurve threadSafe = new AnimationCurve(Settings.HeightDistribution.keys);

            Thread t = new Thread(
                () =>
                {
                    // Normalise heights
                    map.Normalise(min, max);

                    map.Biomes = new Biome.Type[width * height];
                    map.Decoration = new Biome.Decoration[width * height];

                    // Now calculate the actual heights from the noise
                    for (int i = 0; i < map.Heights.Length; i++)
                    {
                        if (Settings.UseCurve)
                        {
                            map.Heights[i] = threadSafe.Evaluate(map.Heights[i]);
                        }

                        map.Heights[i] *= Settings.HeightMultiplier;

                        // Lake has first priority
                        if (Settings.Lake.Do && map.LakeHeights[i] < 0.1f)
                        {
                            map.Heights[i] -= (map.LakeHeights[i] * Settings.Lake.Multiplier);
                            map.Biomes[i] = Settings.Lake.Biome;
                        }
                        // Then bunker
                        else if (Settings.Bunker.Do && map.BunkerHeights[i] < 0.15f)
                        {
                            map.Heights[i] -= (map.BunkerHeights[i] * Settings.Bunker.Multiplier);
                            map.Biomes[i] = Settings.Bunker.Biome;
                        }
                        // Then other land
                        else
                        {
                            // Should be 
                            if (map.Heights[i] < 0.1f)
                            {
                                map.Heights[i] = 0.1f;
                                map.Biomes[i] = Settings.MainBiome;
                            }
                            else
                            {
                                map.Biomes[i] = Settings.MainBiome;
                            }




                            // Do holes next
                            //List<FloodFillBiome> holes = new List<FloodFillBiome>();

                            // Should do decoration here 
                            // TODO
                            // out List<WorldObjectData> worldObjects

                            // Move sampling into here
                        }
                    }
                }
                );
            threads.Add(t);
            t.Start();

        }

        // Wait for threads to complete
        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }




        Debug.Log("* Second pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
            yield break;
        last = DateTime.Now;


        // THIRD PASS

        // Construct textures and meshes
        // This needs to be done in the main thread

        List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();


        foreach (TerrainMap map in maps.Values)
        {
            Texture2D colourMap = TextureGenerator.GenerateBiomeColourMap(map, TextureSettings);

            //Debug.Log(map);
            //Debug.Log(map.Heights);
            //Debug.Log(map.Heights[0]);
            //yield break;


            MeshGenerator.MeshData meshData = null;
            MeshGenerator.UpdateMeshData(ref meshData, map, localVertexPositions);

            Mesh mesh = null;
            meshData.UpdateMesh(ref mesh, MeshSettings);

            // Create the new chunk
            terrainChunks.Add(new TerrainChunkData(map.Chunk.x, map.Chunk.y, map.Bounds.center, map.Biomes, width, height, colourMap, mesh, new List<WorldObjectData>()));
        }

        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(Seed, terrainChunks, new List<HoleData>(), Settings.name);




        // FINISHED GENERATING
        Debug.Log("* Third pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
            yield break;
        last = DateTime.Now;

        Debug.Log("* Generated terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");

        OnAbortThreads.RemoveListener(a);
        IsGenerating = false;

        // Callback when done
        callback(terrain);
    }



    private void GenerateTerrainMapRawData(Vector2Int chunk, int seed, Bounds chunkBounds, int width, int height, in Vector3[] localVertexPositions, out TerrainMap map, out float minHeight, out float maxHeight)
    {
        // Heights
        float[] heightsRaw = Noise.GetSimplex(Settings.NoiseMain, seed, chunkBounds.min, in localVertexPositions, width, height, out minHeight, out maxHeight);

        // Bunkers
        int bunkerSeed = seed.ToString().GetHashCode();
        float[] bunkerHeights = Noise.GetPerlin(Settings.NoiseBunker, bunkerSeed, chunkBounds.min, in localVertexPositions, width, height, out float _, out float _);

        // Lakes
        int lakeSeed = bunkerSeed.ToString().GetHashCode();
        float[] lakeHeights = Noise.GetPerlin(Settings.NoiseLake, lakeSeed, chunkBounds.min, in localVertexPositions, width, height, out float _, out float _);

        // Tree mask
        int treeSeed = lakeSeed.ToString().GetHashCode();
        bool[] treeMask = Noise.GetPerlinMask(Settings.NoiseTree, treeSeed, chunkBounds.min, in localVertexPositions, width, height, Settings.Trees.NoiseThresholdMinMax, out float _, out float _);

        // Rock mask
        int rockSeed = treeSeed.ToString().GetHashCode();
        bool[] rockMask = Noise.GetPerlinMask(Settings.NoiseRock, rockSeed, chunkBounds.min, in localVertexPositions, width, height, Settings.Rocks.NoiseThresholdMinMax, out float _, out float _);

        map = new TerrainMap(chunk, width, height, chunkBounds)
        {
            Heights = heightsRaw,
            BunkerHeights = bunkerHeights,
            LakeHeights = lakeHeights,
            TreeMask = treeMask,
            RockMask = rockMask
        };
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


    public static Vector3[] CalculateLocalVertexPointsForChunk(Vector3 size, int numSamplePoints)
    {
        Vector3[] localPositions = new Vector3[numSamplePoints * numSamplePoints];
        Vector3 one = size / numSamplePoints;

        for (int y = 0; y < numSamplePoints; y++)
        {
            for (int x = 0; x < numSamplePoints; x++)
            {
                int index = y * numSamplePoints + x;
                localPositions[index] = new Vector3(one.x * x, one.y, one.z * y);
            }
        }

        return localPositions;
    }






}
