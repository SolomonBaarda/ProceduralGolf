using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour
{
    public bool IsGenerating { get; private set; } = false;

    [Header("Settings")]
    public MeshSettings MeshSettings;
    [Space]
    public TerrainSettings Settings;
    [Space]
    public TextureSettings TextureSettings;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;

    public UnityEvent<string> OnGenerationStateChanged = new UnityEvent<string>();


    public void Generate(List<Vector2Int> chunks, GameManager.CourseGenerated callback)
    {
        if (IsGenerating)
        {
            Debug.LogError("Terrain is already being generated");
            return;
        }

        Settings.ValidateValues();
        TextureSettings.ValidateValues();
        MeshSettings.ValidateValues();
        TextureSettings.AddColoursToDictionary();
        if (Settings.TerrainLayers.Count < 1)
        {
            throw new Exception("Terrain settings must contain at least one layer");
        }

        // Get random seed
        if (DoRandomSeed)
        {
            Seed = Noise.RandomSeed;
        }

        IsGenerating = true;


        StartCoroutine(WaitForGenerate(chunks, Seed, callback));
    }

    private IEnumerator WaitForGenerate(List<Vector2Int> chunks, int seed, GameManager.CourseGenerated callback)
    {
        DateTime before = DateTime.Now;
        DateTime last = before;

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
        object threadLock = new object();
        int width = Settings.SamplePointFrequency, height = Settings.SamplePointFrequency;
        Vector3[] localVertexPositions = CalculateLocalVertexPointsForChunk(TerrainChunkManager.ChunkSize, Settings.SamplePointFrequency);
        Dictionary<Vector2Int, ChunkData> data = new Dictionary<Vector2Int, ChunkData>();

        List<(float, float)> terrainLayerHeightsMinMax = new List<(float, float)>();

        float minHeight = float.MaxValue, maxHeight = float.MinValue;
        List<Green> greens = new List<Green>();

        List<CourseData> courseData = new List<CourseData>();
        List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();


        // First PASS
        OnGenerationStateChanged.Invoke("First pass: generating random noise");
        {
            for (int i = 0; i < Settings.TerrainLayers.Count; i++)
            {
                terrainLayerHeightsMinMax.Add((float.MaxValue, float.MinValue));
            }

            foreach (Vector2Int chunk in chunks)
            {
                StartThread(threads, "Pass 1 (" + chunk.x + "," + chunk.y + ")", new Thread(() =>
                {
                    Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

                    GenerateTerrainMapRawData(chunk, seed, chunkBounds, width, height, in localVertexPositions, out TerrainMap map);

                    // Gain access to the critical region once we have calculated the data
                    lock (threadLock)
                    {
                        data.Add(chunk, new ChunkData() { TerrainMap = map });

                        // Update the min and max for each of the layers
                        for (int i = 0; i < Settings.TerrainLayers.Count; i++)
                        {
                            if (map.Layers[i].Min < terrainLayerHeightsMinMax[i].Item1)
                            {
                                terrainLayerHeightsMinMax[i] = (map.Layers[i].Min, terrainLayerHeightsMinMax[i].Item2);
                            }
                            if (map.Layers[i].Max > terrainLayerHeightsMinMax[i].Item2)
                            {
                                terrainLayerHeightsMinMax[i] = (terrainLayerHeightsMinMax[i].Item1, map.Layers[i].Max);
                            }
                        }
                    }
                }));
            }

            yield return WaitForThreadsToComplete(threads);
            UpdatePassTimer(ref last, "First");
        }

        // Second PASS
        OnGenerationStateChanged.Invoke("Second pass: calculating terrain values");
        {
            foreach (ChunkData d in data.Values)
            {
                StartThread(threads, "Pass 2 (" + d.TerrainMap.Chunk.x + "," + d.TerrainMap.Chunk.y + ")", new Thread(() =>
                {
                    TerrainMap map = d.TerrainMap;

                    // Normalise each of the noise layers
                    map.NormaliseLayers(terrainLayerHeightsMinMax);

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
                            if (map.Heights[index] < minHeight)
                            {
                                minHeight = map.Heights[index];
                            }
                            if (map.Heights[index] > maxHeight)
                            {
                                maxHeight = map.Heights[index];
                            }
                        }
                    }

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
                                newGreens.Add(FloodFill(map, ref checkedFloodFill, x, y));
                            }
                        }
                    }

                    lock (threadLock)
                    {
                        greens.AddRange(newGreens);
                    }

                    // Now that biomes have been assigned, we can calculate the procedural object positions
                    map.WorldObjects = new List<TerrainMap.WorldObjectData>();
                    if (atLeastOneObject)
                    {
                        GenerateTerrainMapProceduralObjects(map, Settings.PoissonSamplingRadius, Settings.PoissonSamplingIterations);
                    }
                }));
            }

            yield return WaitForThreadsToComplete(threads);
            UpdatePassTimer(ref last, "Second");
        }

        // Third PASS
        OnGenerationStateChanged.Invoke("Third pass: calculate mesh data and merge greens");
        {
            // Do this calculation in a thread so that it is done in the background 
            StartThread(threads, "Pass 3: merge greens", new Thread(() =>
            {
                DateTime a = DateTime.Now;
                int greensBefore = greens.Count;

                // Merge greens from seperate chunks
                foreach (Green original in greens)
                {
                    if (!original.ToBeDeleted && original.PointsOnEdge.Count > 0)
                    {
                        foreach (Green toMerge in greens)
                        {
                            if (!original.Equals(toMerge) && !toMerge.ToBeDeleted && toMerge.PointsOnEdge.Count > 0 && ContainsAnySharedPoints(original, toMerge))
                            {
                                toMerge.ToBeDeleted = true;

                                // Add the vertices
                                original.Points.AddRange(toMerge.Points);
                                original.PointsOnEdge.AddRange(toMerge.PointsOnEdge);
                            }

                            bool ContainsAnySharedPoints(Green a, Green b)
                            {
                                int total = 0;

                                foreach (Green.Point p in b.PointsOnEdge)
                                {
                                    // Removed all shared points from a
                                    // Since a is the green being merged, this will speed up future calls 
                                    // as we will have to compare less elements in the list
                                    total += a.PointsOnEdge.RemoveAll(other => TerrainMap.IsSharedPositionOnBorder(p.Chunk, p.indexX, p.indexY, other.Chunk, other.indexX, other.indexY, width, height));
                                }

                                return total > 0;
                            }
                        }
                    }
                }

                greens.RemoveAll(x => x.ToBeDeleted || x.Points.Count < Settings.GreenMinVertexCount);

                Logger.Log($"* Greens: {greensBefore} reduced to {greens.Count} in {(DateTime.Now - a).TotalSeconds.ToString("0.0")} seconds");
            }));


            foreach (ChunkData d in data.Values)
            {
                // Use height curve to calculate new height distribution
                AnimationCurve threadSafe = new AnimationCurve(Settings.HeightDistribution.keys);

                StartThread(threads, "Pass 3 (" + d.TerrainMap.Chunk.x + "," + d.TerrainMap.Chunk.y + ")", new Thread(() =>
                {
                    TerrainMap map = d.TerrainMap;

                    // Normalise each of the noise layers
                    map.NormaliseHeights(minHeight, maxHeight);

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
                    MeshGenerator.MeshData meshData = null;
                    MeshGenerator.UpdateMeshData(ref meshData, map, localVertexPositions);
                    meshData.GenerateMeshLODData(MeshSettings);

                    lock (threadLock)
                    {
                        // Add the mesh data 
                        data[map.Chunk].MeshData = meshData;
                    }
                }));
            }

            yield return WaitForThreadsToComplete(threads);
            UpdatePassTimer(ref last, "Third");
        }

        // Fourth PASS
        OnGenerationStateChanged.Invoke("Fourth pass: calculate holes and generate texture data");
        {
            System.Random r = new System.Random(0);

            foreach (Green g in greens)
            {
                StartThread(threads, "Pass 3: calculate holes", new Thread(() =>
                {
                    Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

                    ChunkData d = data[g.Points[0].Chunk];
                    Vector3 min = d.TerrainMap.Bounds.min + d.MeshData.Vertices[g.Points[0].indexY * d.MeshData.Width + g.Points[0].indexX], max = min;

                    foreach (Green.Point p in g.Points)
                    {
                        d = data[p.Chunk];
                        Vector3 pos = d.TerrainMap.Bounds.min + d.MeshData.Vertices[p.indexY * d.MeshData.Width + p.indexX];

                        if (pos.x < min.x)
                            min.x = pos.x;
                        if (pos.z < min.z)
                            min.z = pos.z;
                        if (pos.x > max.x)
                            max.x = pos.x;
                        if (pos.z > max.z)
                            max.z = pos.z;
                    }
                    Vector3 size = max - min;

                    List<Vector2> localPoints = PoissonDiscSampling.GenerateLocalPoints(Settings.MinDistanceBetweenHoles, new Vector2(size.x, size.z), Seed);

                    g.PossibleHoles = new List<Vector3>();
                    foreach (Vector2 local in localPoints)
                    {
                        Vector3 world = min + new Vector3(local.x, 0, local.y);

                        // Calculate the chunk for this world position
                        Vector2Int chunk = TerrainChunkManager.WorldToChunk(world);

                        ChunkData pointChunkData = data[chunk];
                        TerrainMap map = pointChunkData.TerrainMap;

                        // Get the closest index for this point
                        if (Utils.GetClosestIndex(world, map.Bounds.min, map.Bounds.max, map.Width, map.Height, out int x, out int y))
                        {
                            int index = y * map.Width + x;
                            Biome.Type t = map.Biomes[index];

                            // Set the correct height
                            world.y = pointChunkData.MeshData.Vertices[index].y;

                            // Then check biome is correct for this point
                            if (Settings.ValidHoleBiomes.Contains(t))
                            {
                                g.PossibleHoles.Add(world);
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to get closest index for a point");
                        }
                    }

                    // Ensure there are at least 2 points
                    if (g.PossibleHoles.Count < 2)
                    {
                        g.ToBeDeleted = true;
                        return;
                    }

                    // Find furthest away points for start and finish
                    float curSqrMag = 0;
                    g.Start = g.PossibleHoles[0];
                    g.Hole = g.PossibleHoles[1];
                    foreach (Vector3 first in g.PossibleHoles)
                    {
                        foreach (Vector3 second in g.PossibleHoles)
                        {
                            if (!first.Equals(second))
                            {
                                float sqrMag = (second - first).sqrMagnitude;
                                if (sqrMag > curSqrMag)
                                {
                                    curSqrMag = sqrMag;
                                    g.Start = first;
                                    g.Hole = second;
                                }
                            }
                        }
                    }

                    Debug.DrawRay(g.Start, Vector3.up * 100, c, 1000);
                    Debug.DrawRay(g.Hole, Vector3.up * 100, c, 1000);

                    // Create the course data object
                    lock (threadLock)
                    {
                        courseData.Add(new CourseData(g.Start, g.Hole));
                    }
                }));
            }

            yield return WaitForThreadsToComplete(threads);
            UpdatePassTimer(ref last, "Fourth");

            greens.RemoveAll(x => x.ToBeDeleted);
        }

        // Fifth PASS
        OnGenerationStateChanged.Invoke("Fifth pass: constructing meshes");
        {
            foreach (ChunkData d in data.Values)
            {
                StartThread(threads, $"Pass 5: update world objects", new Thread(() =>
                {
                    // Update the world object data
                    Dictionary<GameObject, List<(Vector3, Vector3)>> worldObjectDictionary = new Dictionary<GameObject, List<(Vector3, Vector3)>>();
                    foreach (TerrainMap.WorldObjectData w in d.TerrainMap.WorldObjects)
                    {
                        if (!worldObjectDictionary.TryGetValue(w.Prefab, out List<(Vector3, Vector3)> positions))
                        {
                            positions = new List<(Vector3, Vector3)>();
                        }

                        Vector3 world = d.TerrainMap.Bounds.min + w.LocalPosition;
                        world.y = d.MeshData.Vertices[w.ClosestIndexY * d.TerrainMap.Width + w.ClosestIndexX].y;

                        // Add the new position
                        positions.Add((world, w.Rotation));
                        worldObjectDictionary[w.Prefab] = positions;
                    }
                    // Now add the data to the correct objects
                    List<WorldObjectData> worldObjects = new List<WorldObjectData>();
                    foreach (KeyValuePair<GameObject, List<(Vector3, Vector3)>> pair in worldObjectDictionary)
                    {
                        worldObjects.Add(new WorldObjectData()
                        {
                            Prefab = pair.Key,
                            WorldPositions = pair.Value,
                        });
                    }

                    // Generate the texture data
                    TextureGenerator.TextureData textureData = TextureGenerator.GenerateTextureDataForTerrainMap(d.TerrainMap, TextureSettings);

                    lock (threadLock)
                    {
                        data[d.TerrainMap.Chunk].WorldObjects = worldObjects;
                        data[d.TerrainMap.Chunk].TextureData = textureData;
                    }
                }));
            }
            yield return WaitForThreadsToComplete(threads);
            UpdatePassTimer(ref last, "Fifth");
        }





        OnGenerationStateChanged.Invoke("Sixth pass: constructing meshes");
        {
            // Construct textures and meshes
            // This needs to be done in the main thread
            foreach (ChunkData d in data.Values)
            {
                // Generate the mesh
                Mesh mesh = null;
                d.MeshData.ApplyLODTOMesh(ref mesh);
                // Optimise it
                MeshGenerator.OptimiseMesh(ref mesh);

                // Generate the texture
                Texture2D colourMap = TextureGenerator.GenerateTextureFromData(d.TextureData);
                terrainChunks.Add(new TerrainChunkData(d.TerrainMap.Chunk.x, d.TerrainMap.Chunk.y, d.TerrainMap.Bounds.center, d.TerrainMap.Biomes, width, height, colourMap, mesh, d.WorldObjects));

                yield return null;
            }

            //MapData mapData = GenerateMap(data);
            //then Save To Disk as PNG
            //byte[] bytes = mapData.Map.EncodeToPNG();
            //File.WriteAllBytes(Application.dataPath + "/map.png", bytes);


            // Create the object and set the data
            TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
            terrain.SetData(Seed, terrainChunks, courseData, Settings.name);


            // FINISHED GENERATING
            UpdatePassTimer(ref last, "Sixth");

            double totalTime = (DateTime.Now - before).TotalSeconds;
            string message = $"Finished generating terrain. Completed in {totalTime.ToString("0.0")} seconds";
            Logger.Log(message);
            OnGenerationStateChanged.Invoke(message);

            IsGenerating = false;

            // Callback when done
            callback(terrain);
        }
    }


    private static IEnumerator WaitForThreadsToComplete(List<Thread> threads)
    {
        // Wait for threads to complete
        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }
    }

    private static void UpdatePassTimer(ref DateTime last, string passName)
    {
        Logger.Log($"* {passName} pass: { (DateTime.Now - last).TotalSeconds.ToString("0.0") } seconds.");
        last = DateTime.Now;
    }

    private static void StartThread(List<Thread> threads, string name, Thread thread)
    {
        thread.Name = name;
        thread.IsBackground = true;

        lock (threads)
        {
            thread.Start();
            threads.Add(thread);
        }
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
                if (Utils.GetClosestIndex(localPosition, Vector3.zero, map.Bounds.size, map.Width, map.Height, out int x, out int y))
                {
                    Biome.Type biome = map.Biomes[y * map.Width + x];

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
    }

    private MapData GenerateMap(Dictionary<Vector2Int, ChunkData> data)
    {
        KeyValuePair<Vector2Int, ChunkData> first = data.First();
        Vector2Int min = first.Key, max = min;
        foreach (Vector2Int key in data.Keys)
        {
            if (key.x < min.x)
                min.x = key.x;
            if (key.y < min.y)
                min.y = key.y;
            if (key.x > max.x)
                max.x = key.x;
            if (key.y > max.y)
                max.y = key.y;
        }

        TextureGenerator.TextureData[,] textures = new TextureGenerator.TextureData[Mathf.Abs(max.x - min.x) + 1, Mathf.Abs(max.y - min.y) + 1];
        foreach (KeyValuePair<Vector2Int, ChunkData> pair in data)
        {
            textures[pair.Key.x - min.x, pair.Key.y - min.y] = pair.Value.TextureData;
        }

        TextureGenerator.TextureData d = TextureGenerator.CombineChunkTextureData(textures, first.Value.TextureData.Width, first.Value.TextureData.Height, TextureSettings);
        Texture2D map = TextureGenerator.GenerateTextureFromData(d);


        return new MapData()
        {
            Map = map,
            MinWorldPos = data[min].TerrainMap.Bounds.min,
            MaxWorldPox = data[max].TerrainMap.Bounds.max,
        };
    }

    private static void FixProceduralObjectsOnChunkBorders(TerrainMap chunk, List<TerrainMap> neighbours, float minRadius)
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

    private static Green FloodFill(TerrainMap map, ref bool[] checkedFloodFill, int x, int y)
    {
        Queue<(int, int)> q = new Queue<(int, int)>();

        Green green = new Green();
        q.Enqueue((x, y));

        // Each element n of Q
        while (q.Count > 0)
        {
            (int, int) pos = q.Dequeue();

            for (int west = pos.Item1; west >= 0 && map.Greens[pos.Item2 * map.Width + west]; west--)
            {
                UpdateGreenPositionHorizontal(map, ref checkedFloodFill, green, q, west, pos.Item2);
            }
            for (int east = pos.Item1; east < map.Width && map.Greens[pos.Item2 * map.Width + east]; east++)
            {
                UpdateGreenPositionHorizontal(map, ref checkedFloodFill, green, q, east, pos.Item2);
            }
        }

        return green;
    }

    private static void UpdateGreenPositionHorizontal(TerrainMap map, ref bool[] checkedFloodFill, Green green, Queue<(int, int)> q, int x, int y)
    {
        int index = y * map.Width + x;
        if (!checkedFloodFill[index])
        {
            checkedFloodFill[index] = true;
            Green.Point p = new Green.Point() { Chunk = map.Chunk, indexX = x, indexY = y };
            green.Points.Add(p);
            if (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1)
            {
                green.PointsOnEdge.Add(p);
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


    public static HashSet<Vector2Int> GetAllPossibleNearbyChunks(Vector3 position, int chunks)
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


    public static HashSet<Vector2Int> GetAllPossibleNearbyChunks(Vector3 position, float radius)
    {
        return GetAllPossibleNearbyChunks(position, Mathf.RoundToInt(radius / TerrainChunkManager.ChunkSizeWorldUnits));
    }








    private static Vector3[] CalculateLocalVertexPointsForChunk(Vector3 size, int numSamplePoints)
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





    private class ChunkData
    {
        public TerrainMap TerrainMap;
        public MeshGenerator.MeshData MeshData;
        public TextureGenerator.TextureData TextureData;
        public List<WorldObjectData> WorldObjects;
    }


}
