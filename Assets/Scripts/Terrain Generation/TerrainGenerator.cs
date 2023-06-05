using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour, IManager
{
    [Header("Settings")]
    public MeshSettings MeshSettings;
    [Space]
    public TerrainSettings TerrainSettings;
    [Space]
    public TextureSettings TextureSettings;

    [Space]
    public GenerationSettings CurrentSettings;

    public bool IsGenerating { get; private set; } = false;


    private Dictionary<Vector2Int, ChunkData> data = new Dictionary<Vector2Int, ChunkData>();


    //private List<Green> greens = new List<Green>();
    //private List<CourseData> courseData = new List<CourseData>();
    //private List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();



    private void ClearGenerationData()
    {
        //threads.Clear();
        data.Clear();
        //greens.Clear();
        //courseData.Clear();
        //terrainChunks.Clear();
    }



    public void Generate(GenerationSettings settings, int initialGenerationRadius, GameManager.CourseGenerated callback)
    {
        if (IsGenerating)
        {
            Debug.LogError("Terrain is already being generated");
            return;
        }

        TerrainSettings.ValidateValues();
        TextureSettings.ValidateValues();
        MeshSettings.ValidateValues();
        TextureSettings.AddColoursToDictionary();
        ClearGenerationData();

        if (TerrainSettings.TerrainLayers.Count < 1)
        {
            throw new Exception("Terrain settings must contain at least one layer");
        }

        CurrentSettings = settings;

        WaitForGenerate(initialGenerationRadius, callback);
    }

    private static void For(int fromInclusive, int toExclusive, Action<int> body)
    {
        // Parallel
#if false

        Parallel.For(fromInclusive, toExclusive, body);

#else
        // Sequential

        for (int i = fromInclusive; i < toExclusive; i++)
        {
            body.Invoke(i);
        }

#endif
    }

    private void WaitForGenerate(int initialGenerationRadius, GameManager.CourseGenerated callback)
    {
        DateTime startTimestamp = DateTime.Now, lastTimestamp = startTimestamp;
        IsGenerating = true;

        bool atLeastOneObject = false;
        for (int i = 0; i < TerrainSettings.ProceduralObjects.Count; i++)
        {
            if (TerrainSettings.ProceduralObjects[i].Do)
            {
                atLeastOneObject = true;
                break;
            }
        }
        if (!atLeastOneObject)
            Debug.LogError("No procedural objects have been added");



        // TERRAIN LAYERS


        // Construct the terrain map for the whole course
        TerrainMap map = new TerrainMap(initialGenerationRadius * 2 * TerrainSettings.SamplePointFrequency, initialGenerationRadius * 2 * TerrainSettings.SamplePointFrequency);

        Vector2 offset = Vector2.zero;
        Vector2 distanceBetweenNoiseSamples = new Vector2(TerrainChunkManager.ChunkSize.x, TerrainChunkManager.ChunkSize.z) / (TerrainSettings.SamplePointFrequency - 1);

        // Get all the noise layers for the terrain
        For(0, TerrainSettings.TerrainLayers.Count, (int index) =>
        {
            TerrainSettings.Layer layerSettings = TerrainSettings.TerrainLayers[index];

            // Only generate the noise if this layer uses it
            if (!layerSettings.ShareOtherLayerNoise)
            {
                int seed = CurrentSettings.Seed;
                for (int i = 0; i < index; i++)
                {
                    seed = seed.ToString().GetHashCode();
                }

                // Generate the noise for this layer and normalise it
                float min = float.MaxValue;
                float max = float.MinValue;
                terrainLayer.Noise = Noise.GetNoise(layerSettings.Settings, seed, offset, in distanceBetweenNoiseSamples, map.Width, map.Height, ref min, ref max);
                Noise.NormaliseNoise(ref terrainLayer.Noise, min, max);
            }

            TerrainMap.Layer terrainLayer = new TerrainMap.Layer();


            map.Layers.Add(terrainLayer);
        });


        int wasd = 3;


        // TERRAIN HEIGHTS


        // Now calculate the actual heights from the noise and the biomes

        For(0, map.Heights.Length, (int index) =>
        {
            // Set the default biome and height
            map.Biomes[index] = TerrainSettings.MainBiome;
            map.Heights[index] = 0.0f;

            for (int currentLayerIndex = 0; currentLayerIndex < map.Layers.Count; currentLayerIndex++)
            {
                TerrainSettings.Layer layerSettings = TerrainSettings.TerrainLayers[currentLayerIndex];
                TerrainMap.Layer currentLayer = map.Layers[currentLayerIndex];

                // Set the reference to be another layer if we are sharing noise
                if (layerSettings.ShareOtherLayerNoise)
                {
                    currentLayer = map.Layers[layerSettings.LayerIndexShareNoise];
                }

                if (
                    layerSettings.Apply &&
                    currentLayer.Noise[index] >= layerSettings.NoiseThresholdMin &&
                    currentLayer.Noise[index] <= layerSettings.NoiseThresholdMax
                )
                {
                    // Check that the mask is valid if we are using it 
                    bool maskvalid = true;
                    if (layerSettings.UseMask)
                    {
                        for (int j = 0; j < layerSettings.Masks.Count; j++)
                        {
                            TerrainSettings.Layer mask = TerrainSettings.TerrainLayers[layerSettings.Masks[j].LayerIndex];
                            TerrainMap.Layer maskValues = map.Layers[layerSettings.Masks[j].LayerIndex];
                            // Mask is not valid here
                            if (
                                !(maskValues.Noise[index] >= layerSettings.Masks[j].NoiseThresholdMin &&
                                maskValues.Noise[index] <= layerSettings.Masks[j].NoiseThresholdMax)
                            )
                            {
                                maskvalid = false;
                                break;
                            }
                        }
                    }

                    if (!layerSettings.UseMask || maskvalid)
                    {
                        // None biome layer will not effect the final biome
                        if (layerSettings.Biome != Biome.Type.None)
                        {
                            map.Biomes[index] = layerSettings.Biome;
                        }

                        float value = currentLayer.Noise[index] * layerSettings.Multiplier;

                        switch (layerSettings.CombinationMode)
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

                        // Clamp height to zero after this layer has been applied
                        if (layerSettings.ClampHeightToZero && map.Heights[index] < 0.0f)
                        {
                            map.Heights[index] = 0.0f;
                        }
                    }
                }
            }

            // Ensure height map can't go below 0
            if (TerrainSettings.ForceMinHeightZero && map.Heights[index] < 0.0f)
            {
                map.Heights[index] = 0;
            }



            // Calculate if this point can be a green
            map.Greens[index] = false;

            // Set the green boolean flood array
            foreach (TerrainSettings.Green g in TerrainSettings.Greens)
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
        });


#if false


        // SEQUENTIAL

        // Calculate the greens 
        List<Green> greens = new List<Green>();
        bool[] checkedFloodFill = new bool[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                int index = (y * map.Width) + x;
                if (!checkedFloodFill[index] && map.Greens[index])
                {
                    greens.Add(FloodFill(map, ref checkedFloodFill, x, y));
                }
            }
        }



        // SEQUENTIAL

        // Now that biomes have been assigned, we can calculate the procedural object positions
        map.WorldObjects = new List<TerrainMap.WorldObjectData>();
        if (atLeastOneObject)
        {
            GenerateTerrainMapProceduralObjects(map, TerrainSettings.PoissonSamplingRadius, TerrainSettings.PoissonSamplingIterations);
        }





        // Use height curve to calculate new height distribution
        AnimationCurve threadSafe = new AnimationCurve(TerrainSettings.HeightDistribution.keys);

        // Find the min and max heights from the terrain map
        float minHeight = map.Heights[0];
        float maxHeight = minHeight;
        foreach (float height in map.Heights)
        {
            if (height > maxHeight) { maxHeight = height; }

            if (height < minHeight) { minHeight = height; }
        }

        // Normalise the height map
        map.NormaliseHeights(minHeight, maxHeight);

        // Now calculate the final height for the vertex
        for (int index = 0; index < map.Heights.Length; index++)
        {
            if (TerrainSettings.UseCurve)
            {
                map.Heights[index] = threadSafe.Evaluate(map.Heights[index]);
            }

            map.Heights[index] *= TerrainSettings.HeightMultiplier;
        }


        // Now subdivide the data into chunks


        // Now Calculate the mesh data for the chunk

        int chunkSize = TerrainSettings.SamplePointFrequency;
        for (int chunkY = 0; chunkY < map.Height / chunkSize; chunkY++)
        {
            for (int chunkX = 0; chunkX < map.Width / chunkSize; chunkX++)
            {
                MeshGenerator.MeshData meshData = new MeshGenerator.MeshData(chunkSize, chunkSize);

                for (int heightY = 0; heightY < chunkSize; chunkY++)
                {
                    for (int heightX = 0; heightX < chunkSize; chunkX++)
                    {
                        int chunkRelativeIndex = (heightY * chunkSize) + heightX;
                        int heightsIndex = (((chunkY * chunkSize) - chunkY + heightY) * map.Width) + (chunkX * chunkSize) - chunkX + heightX;

                        meshData.Vertices[chunkRelativeIndex] = new Vector3(heightX * distanceBetweenNoiseSamples.x, map.Heights[heightsIndex], heightY * distanceBetweenNoiseSamples.y);
                    }
                }

                meshData.UpdateUVS();
                meshData.GenerateMeshLODData(MeshSettings);

                data.Add(new Vector2Int(chunkX, chunkY), new ChunkData() { MeshData = meshData });
            }
        }











        // Fourth PASS

        FourthPass(map, greens);






        greens.RemoveAll(x => x.ToBeDeleted);




        // Fifth PASS
        Logger.LogTerrainGenerationStartPass(5, "constructing meshes");

        FifthPass();

        yield return WaitForThreadsToComplete(threads);
        Logger.LogTerrainGenerationFinishPass(5, (DateTime.Now - lastTimestamp).TotalSeconds);
        lastTimestamp = DateTime.Now;

        Logger.LogTerrainGenerationStartPass(6, "constructing meshes");

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
            terrainChunks.Add(new TerrainChunkData(d.TerrainMap.Chunk.x, d.TerrainMap.Chunk.y, d.TerrainMap.Bounds.center, d.TerrainMap.Biomes, terrainMapWidth, terrainMapHeight, colourMap, mesh, d.WorldObjects));

            yield return null;
        }

        // TODO in next version
        // Add map perview before actually generating the course
        //MapData mapData = GenerateMap(data);
        //then Save To Disk as PNG
        //byte[] bytes = mapData.Map.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + "/map.png", bytes);


        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(CurrentSettings.Seed, terrainChunks, courseData, TerrainSettings.name);


        Logger.LogTerrainGenerationFinishPass(6, (DateTime.Now - lastTimestamp).TotalSeconds);
        lastTimestamp = DateTime.Now;


        double totalTime = (DateTime.Now - startTimestamp).TotalSeconds;
        string message = $"Finished generating terrain. Completed in {totalTime.ToString("0.0")} seconds";
        Logger.Log(message);

        IsGenerating = false;

        // Callback when done
        callback(terrain);

        //ClearGenerationData();

#endif
    }



    /*
    private void ThirdPassMergeGreens()
    {
        // Do this calculation in a thread so that it is done in the background 
        StartThread(threads, "Pass 3: merge greens", new Thread(() =>
        {
            DateTime a = DateTime.Now;
            int greensBefore = greens.Count;

            // Seems to fix the green merging inconsistency
            // No clue why...
            greens.Sort((x, y) => y.Points.Count.CompareTo(x.Points.Count));

            // Merge greens from seperate chunks
            foreach (Green original in greens)
            {
                if (!original.ToBeDeleted && original.PointsOnEdge.Count > 0)
                {
                    foreach (Green toMerge in greens)
                    {
                        if (!original.Equals(toMerge) && !toMerge.ToBeDeleted && toMerge.PointsOnEdge.Count > 0 && ShouldMergeGreens(original, toMerge))
                        {
                            toMerge.ToBeDeleted = true;

                            // Add the vertices
                            original.Points.AddRange(toMerge.Points);
                            original.PointsOnEdge.AddRange(toMerge.PointsOnEdge);
                        }

                        bool ShouldMergeGreens(Green a, Green b)
                        {
                            int total = 0;

                            foreach (Green.Point p in b.PointsOnEdge)
                            {
                                // Removed all shared points from a
                                // Since a is the green being merged, this will speed up future calls 
                                // as we will have to compare less elements in the list
                                total += a.PointsOnEdge.RemoveAll(other => TerrainMap.IsSharedPositionOnBorder(p.Chunk, p.indexX, p.indexY, other.Chunk, other.indexX, other.indexY, terrainMapWidth, terrainMapHeight));
                            }

                            return total > 0;
                        }
                    }
                }
            }

            greens.RemoveAll(x => x.ToBeDeleted || x.Points.Count < TerrainSettings.GreenMinVertexCount);

            Logger.Log($"* Greens: {greensBefore} reduced to {greens.Count} in {(DateTime.Now - a).TotalSeconds.ToString("0.0")} seconds");
        }));
    }
    */


#if false

    private void FourthPass(TerrainMap map, List<Green> greens)
    {
        System.Random r = new System.Random(0);

        Parallel.ForEach(greens, (Green g) =>
        {
            UnityEngine.Color c = new UnityEngine.Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

            ChunkData d = data[g.Points[0].Chunk];
            Vector3 min = d.TerrainMap.Bounds.min + d.MeshData.Vertices[(g.Points[0].indexY * d.MeshData.Width) + g.Points[0].indexX], max = min;

            foreach (Green.Point p in g.Points)
            {
                d = data[p.Chunk];
                Vector3 pos = d.TerrainMap.Bounds.min + d.MeshData.Vertices[(p.indexY * d.MeshData.Width) + p.indexX];

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

            List<Vector2> localPoints = PoissonDiscSampling.GenerateLocalPoints(TerrainSettings.MinDistanceBetweenHoles, new Vector2(size.x, size.z), CurrentSettings.Seed);

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
                    bool isValidPoint = true;

                    // Check if the point is far away enough from invalid biomes
                    for (int yOffset = -TerrainSettings.AreaToCheckValidHoleBiome; yOffset <= TerrainSettings.AreaToCheckValidHoleBiome; yOffset++)
                    {
                        for (int xOffset = -TerrainSettings.AreaToCheckValidHoleBiome; xOffset <= TerrainSettings.AreaToCheckValidHoleBiome; xOffset++)
                        {
                            int newY = y + yOffset, newX = x + xOffset;

                            if (newY >= 0 && newX >= 0 && newY < terrainMapHeight && newX < terrainMapWidth)
                            {
                                Biome.Type t = map.Biomes[(newY * map.Width) + newX];

                                // Then check biome is correct for this point
                                if (!TerrainSettings.ValidHoleBiomes.Contains(t))
                                {
                                    isValidPoint = false;
                                    break;
                                }
                            }
                        }

                        if (!isValidPoint)
                        {
                            break;
                        }
                    }

                    // We have a valid hole
                    if (isValidPoint)
                    {
                        world.y = pointChunkData.MeshData.Vertices[(y * map.Width) + x].y;
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

            // Create the course data object
            lock (threadLock)
            {
                courseData.Add(new CourseData(g.Start, g.Hole, c));
            }

        }
        );
    }



    private void FifthPass()
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
                    world.y = d.MeshData.Vertices[(w.ClosestIndexY * d.TerrainMap.Width) + w.ClosestIndexX].y;

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
            TerrainSettings.ProceduralObject attempt = TerrainSettings.ProceduralObjects[r.Next(0, TerrainSettings.ProceduralObjects.Count)];

            if (attempt.Do && r.NextDouble() <= attempt.Chance)
            {
                Vector3 localPosition = new Vector3(pos.x, 0, pos.y);
                if (Utils.GetClosestIndex(localPosition, Vector3.zero, map.Bounds.size, map.Width, map.Height, out int x, out int y))
                {
                    Biome.Type biome = map.Biomes[(y * map.Width) + x];

                    // The biome for this position is valid
                    if (attempt.RequiredBiomes.Contains(biome))
                    {
                        // Check that the mask is valid if we are using it
                        bool maskvalid = true;
                        if (attempt.UseMask)
                        {
                            for (int j = 0; j < attempt.Masks.Count; j++)
                            {
                                int index = (y * map.Width) + x;
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



    /*
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
    */

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

            for (int west = pos.Item1; west >= 0 && map.Greens[(pos.Item2 * map.Width) + west]; west--)
            {
                UpdateGreenPositionHorizontal(map, ref checkedFloodFill, green, q, west, pos.Item2);
            }
            for (int east = pos.Item1; east < map.Width && map.Greens[(pos.Item2 * map.Width) + east]; east++)
            {
                UpdateGreenPositionHorizontal(map, ref checkedFloodFill, green, q, east, pos.Item2);
            }
        }

        return green;
    }

    private static void UpdateGreenPositionHorizontal(TerrainMap map, ref bool[] checkedFloodFill, Green green, Queue<(int, int)> q, int x, int y)
    {
        int index = (y * map.Width) + x;
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
            if (newY < map.Height && map.Greens[(newY * map.Width) + x] && !q.Contains((x, newY)))
            {
                q.Enqueue((x, newY));
            }
            // And south
            newY = y - 1;
            if (newY >= 0 && map.Greens[(newY * map.Width) + x] && !q.Contains((x, newY)))
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





#endif


    private static Vector3[] CalculateLocalVertexPointsForChunk(Vector3 size, int numSamplePoints)
    {
        Vector3[] localPositions = new Vector3[numSamplePoints * numSamplePoints];
        Vector3 one = size / (numSamplePoints - 1);

        for (int y = 0; y < numSamplePoints; y++)
        {
            for (int x = 0; x < numSamplePoints; x++)
            {
                int index = (y * numSamplePoints) + x;
                localPositions[index] = new Vector3(one.x * x, 0, one.z * y);
            }
        }

        return localPositions;
    }

    public void Clear()
    {

    }

    public void SetVisible(bool visible)
    {

    }

    private class ChunkData
    {
        public MeshGenerator.MeshData MeshData;
        public TextureGenerator.TextureData TextureData;
        public List<WorldObjectData> WorldObjects;
    }

    [Serializable]
    public class GenerationSettings
    {
        public int Seed = 0;
    }

}
