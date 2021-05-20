﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
    public TerrainChunkManager TerrainChunkManager;

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

        //Debug.ClearDeveloperConsole();
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
        if (Settings.TerrainLayers.Count < 1)
        {
            throw new Exception("Terrain settings must contain at least one layer");
        }

        bool atLeastOneObject = false;
        for (int i = 0; i < Settings.ProceduralObjects.Count; i++)
        {
            if (Settings.ProceduralObjects[i].Do)
            {
                atLeastOneObject = true;
                break;
            }
        }
        if (!atLeastOneObject)
        {
            Debug.LogError("No procedural objects have been added");
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

        //Debug.Log("* First pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
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

                    // Now calculate the actual heights from the noise and the biomes
                    for (int index = 0; index < map.Heights.Length; index++)
                    {
                        // Set the default biome 
                        map.Biomes[index] = Settings.MainBiome;

                        // Set the height and update biomes
                        map.Heights[index] = 0;
                        for (int i = 0; i < map.Layers.Count; i++)
                        {
                            TerrainSettings.Layer s = Settings.TerrainLayers[i];
                            TerrainMap.Layer m = map.Layers[i];
                            if (s.Apply && m.Noise[index] >= s.NoiseThresholdMin && m.Noise[index] <= s.NoiseThresholdMax)
                            {
                                // Check that the mask is valid if we are using it 
                                bool maskvalid = true;
                                if (s.UseMask)
                                {
                                    for (int j = 0; j < s.Masks.Count; j++)
                                    {
                                        TerrainSettings.Layer mask = Settings.TerrainLayers[s.Masks[j].LayerIndex];
                                        TerrainMap.Layer maskValues = map.Layers[s.Masks[j].LayerIndex];
                                        // Mask is not valid here
                                        if (!(maskValues.Noise[index] >= s.Masks[j].NoiseThresholdMin && maskValues.Noise[index] <= s.Masks[j].NoiseThresholdMax))
                                        {
                                            maskvalid = false;
                                            break;
                                        }
                                    }
                                }

                                if (!s.UseMask || maskvalid)
                                {
                                    // None biome layer will not effect the final biome
                                    if (s.Biome != Biome.Type.None)
                                    {
                                        map.Biomes[index] = s.Biome;
                                    }

                                    float value = m.Noise[index] * s.Multiplier;

                                    switch (s.CombinationMode)
                                    {
                                        case TerrainSettings.Layer.Mode.Add:
                                            map.Heights[index] += value;
                                            break;
                                        case TerrainSettings.Layer.Mode.Subtract:
                                            map.Heights[index] -= value;
                                            break;
                                        case TerrainSettings.Layer.Mode.Divide:
                                            map.Heights[index] /= value;
                                            break;
                                        case TerrainSettings.Layer.Mode.Multiply:
                                            map.Heights[index] *= value;
                                            break;
                                        case TerrainSettings.Layer.Mode.Modulus:
                                            map.Heights[index] %= value;
                                            break;
                                        case TerrainSettings.Layer.Mode.Set:
                                            map.Heights[index] = value;
                                            break;
                                    }
                                }
                            }
                        }

                        // Set the green
                        map.Greens[index] = false;
                        foreach (TerrainSettings.Green g in Settings.Greens)
                        {
                            if (g.Do && g.RequiredBiomes.Contains(map.Biomes[index]))
                            {
                                // Check that the mask is valid if we are using it 
                                bool maskvalid = true;
                                if (g.UseMask)
                                {
                                    for (int j = 0; j < g.Masks.Count; j++)
                                    {
                                        TerrainMap.Layer maskValues = map.Layers[g.Masks[j].LayerIndex];

                                        // Mask is not valid here
                                        if (!(maskValues.Noise[index] >= g.Masks[j].NoiseThresholdMin && maskValues.Noise[index] <= g.Masks[j].NoiseThresholdMax))
                                        {
                                            maskvalid = false;
                                            break;
                                        }
                                    }
                                }

                                // Either no mask and will be valid biome, or mask must be valid
                                map.Greens[index] = !g.UseMask || (g.UseMask && maskvalid);

                                if (!map.Greens[index])
                                {
                                    break;
                                }
                            }
                        }


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

                    // Do flood fill stuff here
                    // Small biome cleanup as well

                    // Now that biomes have been assigned, we can calculate the procedural object positions
                    map.WorldObjects = new List<TerrainMap.WorldObjectData>();
                    if (atLeastOneObject)
                    {
                        GenerateTerrainMapProceduralObjects(map, Settings.PoissonSamplingRadius, Settings.PoissonSamplingIterations);
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


        //Debug.Log("* Second pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
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
        List<Green> greens = new List<Green>();

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




                    // Calculate the greens 
                    List<Green> newGreens = new List<Green>();
                    bool[] checkedFloodFill = new bool[map.Width * map.Height];

                    for (int y = 0; y < map.Height; y++)
                    {
                        for (int x = 0; x < map.Width; x++)
                        {
                            int index = y * map.Width + x;
                            if (!checkedFloodFill[index] && map.Greens[index])
                            {
                                newGreens.Add(FloodFill(map, ref checkedFloodFill, data, x, y));
                            }
                        }
                    }

                    lock (threadLock)
                    {
                        // Add the mesh data 
                        meshData.Add(map.Chunk, data);
                        greens.AddRange(newGreens);
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

        // Wait for threads to complete
        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }

        //Debug.Log("* Third pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
        if (aborted)
        {
            OnAbortThreads.RemoveListener(a);
            IsGenerating = false;
            Clear();
            yield break;
        }
        last = DateTime.Now;


        // FOURTH PASS

        Debug.Log("Total greens before: " + greens.Count);

        // Merge greens from seperate chunks
        foreach (Green original in greens)
        {
            if (!original.ToBeDeleted && original.HasVerticesAtEdge)
            {
                foreach (Green toMerge in greens)
                {
                    if (!original.Equals(toMerge) && !toMerge.ToBeDeleted && toMerge.HasVerticesAtEdge && original.Vertices.Overlaps(toMerge.Vertices))
                    {
                        toMerge.ToBeDeleted = true;

                        // Add the vertices
                        original.Vertices.UnionWith(toMerge.Vertices);
                        //toMerge.Vertices.Clear();
                        yield return null;
                    }
                }
            }
        }

        greens.RemoveAll(x => x.ToBeDeleted || x.Vertices.Count == 0);

        Debug.Log("Total greens after: " + greens.Count);

        // Construct textures and meshes
        // This needs to be done in the main thread

        List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();

        foreach (TerrainMap map in maps.Values)
        {
            // Generate the mesh
            meshData.TryGetValue(map.Chunk, out MeshGenerator.MeshData data);
            Mesh mesh = null;
            data.GenerateMesh(ref mesh, MeshSettings);
            yield return null;

            // Update the world object data
            Dictionary<GameObject, List<(Vector3, Vector3)>> worldObjectDictionary = new Dictionary<GameObject, List<(Vector3, Vector3)>>();
            foreach (TerrainMap.WorldObjectData w in map.WorldObjects)
            {
                if (!worldObjectDictionary.TryGetValue(w.Prefab, out List<(Vector3, Vector3)> positions))
                {
                    positions = new List<(Vector3, Vector3)>();
                }

                Vector3 world = map.Bounds.min + w.LocalPosition;
                world.y += data.Vertices[w.ClosestIndexY * map.Width + w.ClosestIndexX].y;

                // Add the new position
                positions.Add((world, w.Rotation));
                worldObjectDictionary[w.Prefab] = positions;
            }
            List<WorldObjectData> worldObjects = new List<WorldObjectData>();
            foreach (KeyValuePair<GameObject, List<(Vector3, Vector3)>> pair in worldObjectDictionary)
            {
                worldObjects.Add(new WorldObjectData()
                {
                    Prefab = pair.Key,
                    WorldPositions = pair.Value,
                });
            }

            // Optimise it and generate the texture
            MeshGenerator.OptimiseMesh(ref mesh);
            Texture2D colourMap = TextureGenerator.GenerateBiomeColourMap(map, TextureSettings);
            terrainChunks.Add(new TerrainChunkData(map.Chunk.x, map.Chunk.y, map.Bounds.center, map.Biomes, width, height, colourMap, mesh, worldObjects));
            yield return null;
        }

        System.Random r = new System.Random(0);

        // Calculate the holes
        List<HoleData> holeData = new List<HoleData>();
        foreach (Green g in greens)
        {
            holeData.Add(new HoleData(g.CalculateStart()));

            Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
            foreach (Vector3 point in g.Vertices)
            {
                Debug.DrawRay(point, Vector3.up * 10, c, 1000);
            }
        }


        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(Seed, terrainChunks, greens, holeData, Settings.name);


        // FINISHED GENERATING
        //Debug.Log("* Fourth pass in " + (DateTime.Now - last).TotalSeconds.ToString("0.0") + " seconds.");
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
    }

    private void GenerateTerrainMapProceduralObjects(TerrainMap map, float minRadius, int iterations)
    {
        int seed = map.Chunk.ToString().GetHashCode();
        System.Random r = new System.Random(seed);

        // Get the local positions
        List<Vector2> localPositions = PoissonDiscSampling.GenerateLocalPoints(minRadius, new Vector2(map.Bounds.size.x, map.Bounds.size.z), seed, iterations);

        // Loop through each position
        foreach (Vector2 pos in localPositions)
        {
            TerrainSettings.ProceduralObject attempt = Settings.ProceduralObjects[r.Next(0, Settings.ProceduralObjects.Count)];

            if (attempt.Do && r.NextDouble() <= attempt.Chance)
            {
                Vector3 localPosition = new Vector3(pos.x, 0, pos.y);
                Biome.Type biome = Utils.GetClosestTo(localPosition, Vector3.zero, map.Bounds.size, in map.Biomes, map.Width, map.Height, out int x, out int y);

                // The biome for this position is valid
                if (attempt.RequiredBiomes.Contains(biome))
                {
                    // Check that the mask is valid if we are using it
                    bool maskvalid = true;
                    if (attempt.UseMask)
                    {
                        for (int j = 0; j < attempt.Masks.Count; j++)
                        {
                            int index = y * map.Width + x;
                            TerrainMap.Layer maskValues = map.Layers[attempt.Masks[j].LayerIndex];
                            // Mask is not valid here
                            if (!(maskValues.Noise[index] >= attempt.Masks[j].NoiseThresholdMin && maskValues.Noise[index] <= attempt.Masks[j].NoiseThresholdMax))
                            {
                                maskvalid = false;
                                break;
                            }
                        }
                    }

                    if (!attempt.UseMask || maskvalid)
                    {
                        // If we get here then this object must be valid at the position
                        map.WorldObjects.Add(new TerrainMap.WorldObjectData()
                        {
                            LocalPosition = localPosition,
                            Rotation = new Vector3(0, (float)r.NextDouble() * 360, 0),
                            Prefab = attempt.Prefabs[r.Next(0, attempt.Prefabs.Count)],
                            ClosestIndexX = x,
                            ClosestIndexY = y,
                        });
                    }
                }
            }
        }
    }

    private void FixProceduralObjectsOnChunkBorders(TerrainMap chunk, List<TerrainMap> neighbours, float minRadius)
    {
        foreach (TerrainMap.WorldObjectData worldObject in chunk.WorldObjects)
        {
            Vector3 world = worldObject.LocalPosition + chunk.Bounds.min;

            foreach (TerrainMap neighbour in neighbours)
            {
                // Remove all that are too close
                neighbour.WorldObjects.RemoveAll(x => (world - x.LocalPosition + neighbour.Bounds.min).sqrMagnitude < minRadius * minRadius);
            }
        }
    }

    private Green FloodFill(TerrainMap map, ref bool[] checkedFloodFill, MeshGenerator.MeshData data, int x, int y)
    {
        Queue<(int, int)> q = new Queue<(int, int)>();

        int index = y * map.Width + x;
        Green green = new Green(map.Bounds.min + data.Vertices[index]);
        q.Enqueue((x, y));

        // Each element n of Q
        while (q.Count > 0)
        {
            (int, int) pos = q.Dequeue();

            for (int west = pos.Item1; west >= 0 && map.Greens[pos.Item2 * map.Width + west]; west--)
            {
                UpdateGreenPositionHorizontal(map, ref checkedFloodFill, data, green, q, west, pos.Item2);
            }
            for (int east = pos.Item1; east < map.Width && map.Greens[pos.Item2 * map.Width + east]; east++)
            {
                UpdateGreenPositionHorizontal(map, ref checkedFloodFill, data, green, q, east, pos.Item2);
            }
        }

        return green;
    }

    private void UpdateGreenPositionHorizontal(TerrainMap map, ref bool[] checkedFloodFill, MeshGenerator.MeshData data, Green green, Queue<(int, int)> q, int x, int y)
    {
        int index = y * map.Width + x;
        if (!checkedFloodFill[index])
        {
            checkedFloodFill[index] = true;
            green.Vertices.Add(map.Bounds.min + data.Vertices[index]);

            // Vertex is on the edge of the map
            if (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1)
            {
                green.HasVerticesAtEdge = true;
            }

            // Check north
            int newY = y + 1;
            if (newY < map.Height && map.Greens[newY * map.Width + x] && !q.Contains((x, newY)))
            {
                q.Enqueue((x, newY));
            }
            // And south
            newY = y - 1;
            if (newY >= 0 && map.Greens[newY * map.Width + x] && !q.Contains((x, newY)))
            {
                q.Enqueue((x, newY));
            }
        }
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








    private Vector3[] CalculateLocalVertexPointsForChunk(Vector3 size, int numSamplePoints)
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
