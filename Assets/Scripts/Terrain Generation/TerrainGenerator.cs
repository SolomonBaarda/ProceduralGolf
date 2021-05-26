using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public TerrainChunkManager TerrainChunkManager;

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


    public void Generate(List<Vector2Int> chunks, GameManager.LoadLevel callback)
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

    private IEnumerator WaitForGenerate(List<Vector2Int> chunks, int seed, GameManager.LoadLevel callback)
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
        Vector3[] localVertexPositions = CalculateLocalVertexPointsForChunk(new Vector3(TerrainChunkManager.ChunkGrid.cellSize.x, 0, TerrainChunkManager.ChunkGrid.cellSize.y), Settings.SamplePointFrequency);
        Dictionary<Vector2Int, ChunkData> data = new Dictionary<Vector2Int, ChunkData>();

        // FIRST PASS
        // Generate the initial chunks
        List<(float, float)> terrainLayerHeightsMinMax = new List<(float, float)>();

        {
            for (int i = 0; i < Settings.TerrainLayers.Count; i++)
            {
                terrainLayerHeightsMinMax.Add((float.MaxValue, float.MinValue));
            }

            foreach (Vector2Int chunk in chunks)
            {
                // Get the chunk bounds
                // This has to be called from the main thread
                Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

                StartThread(threads, "Pass 1 (" + chunk.x + "," + chunk.y + ")", new Thread(() =>
                {
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
            Debug.Log($"* First pass: { (DateTime.Now - last).TotalSeconds.ToString("0.0") } seconds.");
            last = DateTime.Now;
        }




        // SECOND PASS
        float minHeight = float.MaxValue, maxHeight = float.MinValue;
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

                    // Do flood fill stuff here
                    // Small biome cleanup as well

                    // Now that biomes have been assigned, we can calculate the procedural object positions
                    map.WorldObjects = new List<TerrainMap.WorldObjectData>();
                    if (atLeastOneObject)
                    {
                        GenerateTerrainMapProceduralObjects(map, Settings.PoissonSamplingRadius, Settings.PoissonSamplingIterations);
                    }
                }));
            }

            yield return WaitForThreadsToComplete(threads);
            Debug.Log($"* Second pass: { (DateTime.Now - last).TotalSeconds.ToString("0.0") } seconds.");
            last = DateTime.Now;
        }



        // THIRD PASS
        // Normalise final heights and construct mesh data

        List<Green> greens = new List<Green>();

        {
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

                    TextureGenerator.TextureData textureData = TextureGenerator.GenerateTextureDataForTerrainMap(d.TerrainMap, TextureSettings);

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
                                newGreens.Add(FloodFill(map, ref checkedFloodFill, meshData, x, y));
                            }
                        }
                    }

                    lock (threadLock)
                    {
                        // Add the mesh data 
                        data[map.Chunk].MeshData = meshData;
                        data[map.Chunk].TextureData = textureData;
                        greens.AddRange(newGreens);
                    }
                }));
            }

            yield return WaitForThreadsToComplete(threads);
            Debug.Log($"* Third pass: { (DateTime.Now - last).TotalSeconds.ToString("0.0") } seconds.");
            last = DateTime.Now;
        }


        // FOURTH PASS
        {
            // Merge the greens not in the main thread
            StartThread(threads, "Pass 4: merge greens", new Thread(() =>
            {
                int greensBefore = greens.Count;

                // Merge greens from seperate chunks
                foreach (Green original in greens)
                {
                    if (!original.ToBeDeleted && original.PointsOnEdge.Count > 0)
                    {
                        foreach (Green toMerge in greens)
                        {
                            // a.Any(a => b.Contains(a))
                            if (!original.Equals(toMerge) && !toMerge.ToBeDeleted && ContainsAnySharedPoints(original, toMerge))
                            {
                                toMerge.ToBeDeleted = true;

                                // Add the vertices
                                original.Points.AddRange(toMerge.Points);
                                //toMerge.Vertices.Clear();
                            }

                            bool ContainsAnySharedPoints(Green a, Green b)
                            {
                                foreach(Green.Point p in a.PointsOnEdge)
                                {
                                    if(b.PointsOnEdge.Any(x => Green.Point.IsValidNeighbour(p, x)))
                                    {
                                        return true;
                                    }
                                }
                                return false;
                            }
                        }
                    }
                }

                greens.RemoveAll(x => x.ToBeDeleted || x.Points.Count == 0);

                Debug.Log($"* Greens: {greensBefore} reduced to {greens.Count}");
            }));

            // Update the world objects not in the main thread
            StartThread(threads, $"Pass 4: update world objects", new Thread(() =>
            {
                foreach (ChunkData d in data.Values)
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
                        world.y += d.MeshData.Vertices[w.ClosestIndexY * d.TerrainMap.Width + w.ClosestIndexX].y;

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

                    lock (threadLock)
                    {
                        data[d.TerrainMap.Chunk].WorldObjects = worldObjects;
                    }
                }
            }));


            // Construct textures and meshes
            // This needs to be done in the main thread
            List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();

            foreach (ChunkData d in data.Values)
            {
                // Generate the mesh
                Mesh mesh = null;
                d.MeshData.ApplyLODTOMesh(ref mesh);
                // Optimise it
                MeshGenerator.OptimiseMesh(ref mesh);
                // Generate the texture
                Texture2D colourMap = TextureGenerator.GenerateBiomeColourMap(d.TextureData);
                terrainChunks.Add(new TerrainChunkData(d.TerrainMap.Chunk.x, d.TerrainMap.Chunk.y, d.TerrainMap.Bounds.center, d.TerrainMap.Biomes, width, height, colourMap, mesh, d.WorldObjects));

                yield return null;
            }

            yield return WaitForThreadsToComplete(threads);



            // Calculate the holes
            // TODO: move to thread
            List<HoleData> holeData = new List<HoleData>();
            System.Random r = new System.Random(0);
            foreach (Green g in greens)
            {
                holeData.Add(new HoleData(g.CalculateStart()));

                Color c = new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
                foreach (Green.Point point in g.Points)
                {
                    ChunkData d = data[point.Map.Chunk];
                    
                    Debug.DrawRay(d.TerrainMap.Bounds.min + d.MeshData.Vertices[point.indexY * d.MeshData.Width + point.indexX], Vector3.up * 10, c, 1000);
                }
            }




            // Create the object and set the data
            TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
            terrain.SetData(Seed, terrainChunks, greens, holeData, Settings.name);


            // FINISHED GENERATING
            Debug.Log($"* Fourth pass: { (DateTime.Now - last).TotalSeconds.ToString("0.0") } seconds.");
            last = DateTime.Now;

            Debug.Log($"* Generated terrain in { (DateTime.Now - before).TotalSeconds.ToString("0.0") } seconds.");

            IsGenerating = false;

            // Callback when done
            callback(terrain);
        }
    }

    private IEnumerator WaitForThreadsToComplete(List<Thread> threads)
    {
        // Wait for threads to complete
        while (threads.Count > 0)
        {
            threads.RemoveAll((x) => !x.IsAlive);
            yield return null;
        }
    }

    private void StartThread(List<Thread> threads, string name, Thread thread)
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
        Green green = new Green();
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
            //Vector3 pos = map.Bounds.min + data.Vertices[index];
            //green.Vertices.Add(pos);
            Green.Point p = new Green.Point() { Map = map, indexX = x, indexY = y };
            green.Points.Add(p);
            if(x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1)
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





    private class ChunkData
    {
        public TerrainMap TerrainMap;
        public MeshGenerator.MeshData MeshData;
        public TextureGenerator.TextureData TextureData;
        public List<WorldObjectData> WorldObjects;
    }


}
