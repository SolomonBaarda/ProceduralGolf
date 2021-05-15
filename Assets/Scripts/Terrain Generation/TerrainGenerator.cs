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

        Settings.ValidateValues();
        if(Settings.TerrainLayers.Count < 1)
        {
            throw new Exception("Terrain settings must contain at least one layer");
        }

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
        List<(float, float)> minMax = new List<(float, float)>();
        for (int i = 0; i < Settings.TerrainLayers.Count; i++)
        {
            minMax.Add((float.MaxValue, float.MinValue));
        }

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

                        // Update the min and max for each of the layers
                        for (int i = 0; i < Settings.TerrainLayers.Count; i++)
                        {
                            if (map.Layers[i].Min < minMax[i].Item1)
                            {
                                minMax[i] = (map.Layers[i].Min, minMax[i].Item2);
                            }
                            if (map.Layers[i].Max > minMax[i].Item2)
                            {
                                minMax[i] = (minMax[i].Item1, map.Layers[i].Max);
                            }
                        }
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
        float min = float.MaxValue, max = float.MinValue;

        threads.Clear();
        foreach (TerrainMap map in maps.Values)
        {
            Thread t = new Thread(
                () =>
                {
                    // Normalise each of the noise layers
                    map.NormaliseLayers(minMax);
                    map.Biomes = new Biome.Type[width * height];
                    map.Decoration = new Biome.Decoration[width * height];
                    map.Heights = new float[width * height];

                    // Now calculate the actual heights from the noise
                    for (int index = 0; index < map.Heights.Length; index++)
                    {
                        map.Biomes[index] = Settings.MainBiome;
                        map.Heights[index] = 0;

                        for (int i = 0; i < map.Layers.Count; i++)
                        {
                            TerrainSettings.Layer s = Settings.TerrainLayers[i];
                            TerrainMap.Layer m = map.Layers[i];
                            if (s.Apply && m.Noise[index] >= s.NoiseThresholdMinMax.x && m.Noise[index] <= s.NoiseThresholdMinMax.y)
                            {
                                // None biome layer will not effect the final biome
                                if(s.Biome != Biome.Type.None)
                                {
                                    map.Biomes[index] = s.Biome;
                                }
                                
                                switch (s.CombinationMode)
                                {
                                    case TerrainSettings.Layer.Mode.Add:
                                        map.Heights[index] += m.Noise[index] * s.Multiplier;
                                        break;
                                    case TerrainSettings.Layer.Mode.Subtract:
                                        map.Heights[index] -= m.Noise[index] * s.Multiplier;
                                        break;
                                }
                            }
                        }

                        // Do holes next
                        //List<FloodFillBiome> holes = new List<FloodFillBiome>();

                        // Should do decoration here 
                        // TODO
                        // out List<WorldObjectData> worldObjects

                        // Move sampling into here


                        // Get final min max
                        lock (threadLock)
                        {

                            if (map.Heights[index] < min)
                            {
                                min = map.Heights[index];
                            }
                            if (map.Heights[index] > max)
                            {
                                max = map.Heights[index];
                            }
                        }
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
        // Normalise final heights and construct mesh data


        Dictionary<Vector2Int, MeshGenerator.MeshData> meshData = new Dictionary<Vector2Int, MeshGenerator.MeshData>();


        threads.Clear();
        foreach (TerrainMap map in maps.Values)
        {
            // Use height curve to calculate new height distribution
            AnimationCurve threadSafe = new AnimationCurve(Settings.HeightDistribution.keys);

            Thread t = new Thread(
                () =>
                {
                    // Normalise each of the noise layers
                    map.NormaliseHeights(min, max);

                    // Now calculate the final height for the vertex
                    for (int index = 0; index < map.Heights.Length; index++)
                    {
                        if (Settings.UseCurve)
                        {
                            map.Heights[index] = threadSafe.Evaluate(map.Heights[index]);
                        }

                        map.Heights[index] *= Settings.HeightMultiplier;
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
            { Name = "Pass 3 (" + map.Chunk.x + "," + map.Chunk.y + ")" }; ;
            lock (threads)
            {
                threads.Add(t);
                t.Start();
            }
        }




        Debug.Log("* Third pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
        {
            OnAbortThreads.RemoveListener(a);
            IsGenerating = false;
            Clear();
            yield break;
        }
        last = DateTime.Now;


        // FOURTH PASS

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
        Debug.Log("* Fourth pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
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
        map = new TerrainMap(chunk, width, height, chunkBounds) { Layers = new List<TerrainMap.Layer>() };

        // Get all the noise layers
        int prevSeed = seed;
        for (int i = 0; i < Settings.TerrainLayers.Count; i++)
        {
            TerrainSettings.Layer settingsLayer = Settings.TerrainLayers[i];
            TerrainMap.Layer terrainLayer = new TerrainMap.Layer();

            terrainLayer.Noise = Noise.GetNoise(settingsLayer.Settings, prevSeed, chunkBounds.min, in localVertexPositions, width, height, ref terrainLayer.Min, ref terrainLayer.Max);
            prevSeed = prevSeed.ToString().GetHashCode();

            map.Layers.Add(terrainLayer);
        }

        // Tree mask
        int treeSeed = prevSeed.ToString().GetHashCode();
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
