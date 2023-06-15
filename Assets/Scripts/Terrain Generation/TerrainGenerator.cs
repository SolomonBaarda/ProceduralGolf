#define GOLF_PARALLEL_ROUTINES
//#define DEBUG_FLOOD_FILL

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
#if GOLF_PARALLEL_ROUTINES

        // new System.Threading.Tasks.ParallelOptions() { MaxDegreeOfParallelism = 2 }, 
        System.Threading.Tasks.Parallel.For(fromInclusive, toExclusive, body);

#else
        // Sequential

        for (int i = fromInclusive; i < toExclusive; i++)
        {
            body.Invoke(i);
        }

#endif
    }

    private static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
    {
        // Parallel
#if GOLF_PARALLEL_ROUTINES

        System.Threading.Tasks.Parallel.ForEach(source, body);

#else
        // Sequential

        foreach (TSource item in source)
        {
            body.Invoke(item);
        }

#endif
    }

    private void WaitForGenerate(int initialGenerationRadius, GameManager.CourseGenerated callback)
    {
        Debug.Log($"Starting generation using seed {CurrentSettings.Seed}");

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
        float distanceBetweenNoiseSamples = TerrainChunkManager.ChunkSizeWorldUnits / (TerrainSettings.SamplePointFrequency - 1);




        GenerateNoise(map, offset, distanceBetweenNoiseSamples);

        Logger.Log($"*Noise pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;


        // SEQUENTIAL

        // Now that biomes have been assigned, we can calculate the procedural object positions
        map.WorldObjects = !atLeastOneObject ?
            new List<TerrainMap.WorldObjectData>() :
            GenerateTerrainMapProceduralObjects(map, TerrainSettings.PoissonSamplingRadius, TerrainSettings.PoissonSamplingIterations, new Vector2(map.Width * distanceBetweenNoiseSamples, map.Height * distanceBetweenNoiseSamples));


        Logger.Log($"*SEQ pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;


        // TEMP TODO Find the min and max heights from the terrain map
        float minHeight = map.Heights[0];
        float maxHeight = minHeight;
        foreach (float height in map.Heights)
        {
            if (height > maxHeight) { maxHeight = height; }
            if (height < minHeight) { minHeight = height; }
        }

        // SEQUENTIAL

        // Normalise the height map
        map.NormaliseHeights(minHeight, maxHeight);

        // Use height curve to calculate new height distribution
        AnimationCurve threadSafe = new AnimationCurve(TerrainSettings.HeightDistribution.keys);

        // Now calculate the final height for the vertex
        For(0, map.Heights.Length, (int index) =>
        {
            if (TerrainSettings.UseCurve)
            {
                map.Heights[index] = threadSafe.Evaluate(map.Heights[index]);
            }

            map.Heights[index] *= TerrainSettings.HeightMultiplier;
        });

        Logger.Log($"*Renorm pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;



        // Now subdivide the data into chunks
        // Now Calculate the mesh data for the chunk

        int chunkSize = TerrainSettings.SamplePointFrequency;


        ConcurrentDictionary<Vector2Int, ChunkData> data = SplitIntoChunks(map, chunkSize, offset, distanceBetweenNoiseSamples);


        Logger.Log($"*Chunking pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;


        List<Tuple<Vector2Int, Vector2Int>> coursesStartEnd = CalculateCourses(map, CurrentSettings.Seed, data, chunkSize, 100, chunkSize / 8);
        List<CourseData> courses = new List<CourseData>();

        System.Random r = new System.Random(0);

        foreach (Tuple<Vector2Int, Vector2Int> startEnd in coursesStartEnd)
        {
            UnityEngine.Color c = new UnityEngine.Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());

            TerrainMapIndexToChunk(chunkSize, startEnd.Item1, out Vector2Int startChunk, out Vector2Int startIndex);
            TerrainMapIndexToChunk(chunkSize, startEnd.Item2, out Vector2Int endChunk, out Vector2Int endIndex);

            // Get the mesh positions of the start and end pos
            if (data.TryGetValue(startChunk, out ChunkData startChunkData) && data.TryGetValue(endChunk, out ChunkData endChunkData))
            {
                Vector3 start = startChunkData.MeshData.Vertices[(startIndex.y * startChunkData.BiomesWidth) + startIndex.x] + startChunkData.Bounds.min;
                Vector3 end = endChunkData.MeshData.Vertices[(endIndex.y * endChunkData.BiomesWidth) + endIndex.x] + endChunkData.Bounds.min;

                Debug.DrawLine(start, start + (Vector3.up * 100), c, 120);
                Debug.DrawLine(end, end + (Vector3.up * 100), c, 120);

                courses.Add(new CourseData(start, end, c));
            }
            else
            {
                Debug.LogError("Failed to create course as data is missing chunk index");
            }
        }

        Logger.Log($"*Course pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;


        List<TerrainChunkData> terrainChunks = new List<TerrainChunkData>();

        // Construct textures and meshes
        // This needs to be done in the main thread
        foreach (KeyValuePair<Vector2Int, ChunkData> d in data)
        {
            // Generate the mesh
            Mesh mesh = null;
            d.Value.MeshData.ApplyLODTOMesh(ref mesh);
            mesh.name = d.Key.ToString();

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            // Optimise it
            mesh.Optimize();

            // Generate the texture
            Texture2D colourMap = TextureGenerator.GenerateTextureFromData(d.Value.TextureData);

            terrainChunks.Add(new TerrainChunkData(d.Key, d.Value.Bounds, d.Value.Biomes, chunkSize, chunkSize, colourMap, mesh, d.Value.WorldObjects));
        }

        Logger.Log($"*Create mesh pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;

        // TODO in next version
        // Add map perview before actually generating the course
        //MapData mapData = GenerateMap(data);
        //then Save To Disk as PNG
        //byte[] bytes = mapData.Map.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + "/map.png", bytes);


        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(CurrentSettings.Seed, terrainChunks, courses, TerrainSettings.name);


        Logger.Log($"*End pass: {(DateTime.Now - lastTimestamp).TotalSeconds} seconds");
        lastTimestamp = DateTime.Now;



        double totalTime = (DateTime.Now - startTimestamp).TotalSeconds;
        string message = $"Finished generating terrain. Completed in {totalTime.ToString("0.0")} seconds";
        Logger.Log(message);

        IsGenerating = false;

        // Callback when done
        callback(terrain);

        //ClearGenerationData();
    }


    static void TerrainMapIndexToChunk(int chunkSize, Vector2Int terrainMapIndex, out Vector2Int chunk, out Vector2Int chunkRelativeIndex)
    {
        chunk = terrainMapIndex / chunkSize;
        chunkRelativeIndex = new Vector2Int(terrainMapIndex.x % chunkSize, terrainMapIndex.y % chunkSize);
    }

    //static bool ChunkRelativeIndexToTerrainMap(int chunkSize, Vector2Int chunk, Vector2Int chunkRelativeIndex, out Vector2Int terrainMapIndex) { }


    /// <summary>
    /// Generate raw noise, normalise it, combine noise layers, and construct green flood fill boolean array
    /// </summary>
    /// <param name="map"></param>
    /// <param name="offset"></param>
    /// <param name="distanceBetweenNoiseSamples"></param>
    private void GenerateNoise(TerrainMap map, Vector2 offset, float distanceBetweenNoiseSamples)
    {
        for (int i = 0; i < TerrainSettings.TerrainLayers.Count; i++)
        {
            map.Layers.Add(new TerrainMap.Layer(new float[] { }, Biome.Type.None));
        }

        // Get all the noise layers for the terrain
        For(0, TerrainSettings.TerrainLayers.Count, (int index) =>
            {
                TerrainSettings.Layer layerSettings = TerrainSettings.TerrainLayers[index];

                // Only generate the noise if this layer uses it
                //if (!layerSettings.ShareOtherLayerNoise)
                {
                    int seed = CurrentSettings.Seed.GetHashCode() + index.GetHashCode();

                    // Generate the noise for this layer and normalise it
                    float[] noise = Noise.GetNoise(layerSettings.Settings, seed, offset, new Vector2(distanceBetweenNoiseSamples, distanceBetweenNoiseSamples), map.Width, map.Height, out float min, out float max);
                    Noise.NormaliseNoise(ref noise, min, max);

                    map.Layers[index] = new TerrainMap.Layer(noise, layerSettings.Biome);
                }
                //else
                //{
                //    map.Layers.Add(new TerrainMap.Layer(new float[] { }, layerSettings.Biome));
                //}
            });





        // TERRAIN HEIGHTS

        // Now calculate the actual heights from the noise and the biomes

        For(0, map.Heights.Length, (int index) =>
        {
            // Set the default biome and height
            map.Biomes[index] = TerrainSettings.MainBiome;
            map.Heights[index] = 0.0f;

            Vector2Int directionToCentre = new Vector2Int(index % map.Width, index / map.Width) - new Vector2Int(map.Width / 2, map.Height / 2);
            float distanceFalloffRadius = Math.Min(map.Width, map.Height) / 2.0f;
            // 0 - 1 value 
            float distanceFromCentreScaled = Math.Clamp(directionToCentre.sqrMagnitude / (distanceFalloffRadius * distanceFalloffRadius), 0.0f, 1.0f);

            for (int layerIndex = 0; layerIndex < map.Layers.Count; layerIndex++)
            {
                TerrainSettings.Layer layerSettings = TerrainSettings.TerrainLayers[layerIndex];
                TerrainMap.Layer currentLayer = map.Layers[layerIndex];

                // Set the reference to be another layer if we are sharing noise
                if (layerSettings.ShareOtherLayerNoise)
                {
                    currentLayer = map.Layers[layerSettings.LayerIndexShareNoise];
                }

                // Scale noise according to the distance from the origin
                if(layerSettings.UseDistanceFromOriginCurve)
                {
                    // TODO maybe use copy of curve?
                    currentLayer.Noise[index] *= layerSettings.DistanceFromOriginCurve.Evaluate(distanceFromCentreScaled);
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
                        for (int layerMaskIndex = 0; layerMaskIndex < layerSettings.Masks.Count; layerMaskIndex++)
                        {
                            TerrainSettings.Layer mask = TerrainSettings.TerrainLayers[layerSettings.Masks[layerMaskIndex].LayerIndex];
                            TerrainMap.Layer maskValues = map.Layers[layerSettings.Masks[layerMaskIndex].LayerIndex];
                            // Mask is not valid here
                            if (
                                !(maskValues.Noise[index] >= layerSettings.Masks[layerMaskIndex].NoiseThresholdMin &&
                                maskValues.Noise[index] <= layerSettings.Masks[layerMaskIndex].NoiseThresholdMax)
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

                        float value = currentLayer.Noise[index] * layerSettings.Multiplier ;

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
                        foreach (TerrainSettings.Mask greenLayerMask in g.Masks)
                        {
                            TerrainMap.Layer layer = map.Layers[greenLayerMask.LayerIndex];

                            TerrainSettings.Layer layerSettings = TerrainSettings.TerrainLayers[greenLayerMask.LayerIndex];

                            // Set the reference to be another layer if we are sharing noise
                            if (layerSettings.ShareOtherLayerNoise)
                            {
                                layer = map.Layers[layerSettings.LayerIndexShareNoise];
                            }

                            // Mask is not valid here
                            if (!(layer.Noise[index] >= greenLayerMask.NoiseThresholdMin && layer.Noise[index] <= greenLayerMask.NoiseThresholdMax))
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
    }


    /// <summary>
    /// Calculate start/hole positions for each golf course
    /// </summary>
    /// <param name="map"></param>
    /// <param name="greens"></param>
    private List<Tuple<Vector2Int, Vector2Int>> CalculateCourses(TerrainMap map, int seed, ConcurrentDictionary<Vector2Int, ChunkData> data, int chunkSize, int numAttemptsToChooseRandomPositions = 100, int precisionStep = 1)
    {
        // SEQUENTIAL

        // First use flood fill to calculate the courses

        List<List<Vector2Int>> coursePoints = new List<List<Vector2Int>>();
        bool[] checkedFloodFill = new bool[map.Width * map.Height];

        // Increment using the precision step to improve performance 
        // Unlikely to miss any parts of the course
        for (int y = 0; y < map.Height; y += precisionStep)
        {
            for (int x = 0; x < map.Width; x += precisionStep)
            {
                int index = (y * map.Width) + x;

                if (!checkedFloodFill[index] && map.Greens[index])
                {
                    List<Vector2Int> possibleCoursePoints = CalculateCourseFloodFillMain(map, ref checkedFloodFill, x, y);

                    // Ensure only valid courses get added
                    if (possibleCoursePoints.Count > 2 && possibleCoursePoints.Count > TerrainSettings.GreenMinVertexCount)
                    {
                        coursePoints.Add(possibleCoursePoints);
                    }
                }
            }
        }


#if DEBUG_FLOOD_FILL

        Texture2D t = new Texture2D(map.Width, map.Height);

        System.Random r1 = new System.Random(0);

        UnityEngine.Color32[] c = new Color32[map.Width * map.Height];

        For(0, map.Width * map.Height, (int i) =>
        {
            c[i] = TextureSettings.GetColour(map.Biomes[i]);
        });

        foreach (var list in coursePoints)
        {
            UnityEngine.Color32 colour = new UnityEngine.Color32((byte)(r1.NextDouble() * 255), (byte)(r1.NextDouble() * 255), (byte)(r1.NextDouble() * 255), 255);

            ForEach(list, (Vector2Int p) =>
            {
                c[(p.y * map.Width) + p.x] = colour;
            });
        }

        t.SetPixels32(c);
        t.Apply();

        byte[] png = t.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/debug_floodfill.png", png);

        Debug.LogWarning($"Wrote flood fill debug image to \"{Application.dataPath + "/debug_floodfill.png"}\"");

#endif


        // Initialise to correct size so that we don't need to synchronise between threads
        List<Tuple<Vector2Int, Vector2Int>> courses = new List<Tuple<Vector2Int, Vector2Int>>(coursePoints.Count);
        for (int i = 0; i < coursePoints.Count; i++)
        {
            courses.Add(new(Vector2Int.zero, Vector2Int.zero));
        }

        // Calculate the start and end positions for each of those courses
        For(0, coursePoints.Count, (int index) =>
        {
            List<Vector2Int> points = coursePoints[index];

            // Find furthest away points for start and finish
            int curSqrMag = 0;
            Vector2Int start = points[0];
            Vector2Int hole = points[1];
            System.Random r = new System.Random(seed);

            for (int i = 0; i < numAttemptsToChooseRandomPositions; i++)
            {
                Vector2Int first = points[r.Next(points.Count)];
                Vector2Int second = points[r.Next(points.Count)];

                if (!first.Equals(second))
                {
                    int sqrMag = (second - first).sqrMagnitude;
                    if (sqrMag > curSqrMag)
                    {
                        curSqrMag = sqrMag;
                        start = first;
                        hole = second;
                    }
                }
            }

            // Create the course data object
            courses[index] = new(start, hole);
        });


        return courses;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="map"></param>
    private ConcurrentDictionary<Vector2Int, ChunkData> SplitIntoChunks(TerrainMap map, int chunkSize, Vector2 offset, float distanceBetweenNoiseSamples)
    {
        ConcurrentDictionary<Vector2Int, ChunkData> data = new ConcurrentDictionary<Vector2Int, ChunkData>();

        For(0, map.Height / chunkSize, (int chunkY) =>
        {
            for (int chunkX = 0; chunkX < map.Width / chunkSize; chunkX++)
            {
                MeshGenerator.MeshData meshData = new MeshGenerator.MeshData(chunkSize, chunkSize);
                Biome.Type[] biomes = new Biome.Type[chunkSize * chunkSize];

                for (int heightY = 0; heightY < chunkSize; heightY++)
                {
                    for (int heightX = 0; heightX < chunkSize; heightX++)
                    {
                        // Calculate the indexes to map between the chunk and terrain map
                        int localIndex = (heightY * chunkSize) + heightX;
                        int terrainMapIndex = (((chunkY * chunkSize) - chunkY + heightY) * map.Width) + (chunkX * chunkSize) - chunkX + heightX;

                        // Copy the mesh data
                        meshData.Vertices[localIndex] = new Vector3(heightX * distanceBetweenNoiseSamples, map.Heights[terrainMapIndex], heightY * distanceBetweenNoiseSamples);

                        // Copy the biome data
                        biomes[localIndex] = map.Biomes[terrainMapIndex];
                    }
                }

                meshData.UpdateUVS();
                meshData.GenerateMeshLODData(MeshSettings);

                // Compute texture data for chunk
                TextureGenerator.TextureData textureData = TextureGenerator.GenerateTextureDataForChunk(biomes, chunkSize, chunkSize, TextureSettings);

                Vector3 centre = new Vector3((distanceBetweenNoiseSamples * (chunkSize - 1) * chunkX) + offset.x, 0, (distanceBetweenNoiseSamples * (chunkSize - 1) * chunkY) + offset.y);
                Bounds bounds = new Bounds(centre, new Vector3(TerrainChunkManager.ChunkSizeWorldUnits, 0, TerrainChunkManager.ChunkSizeWorldUnits));

                data.TryAdd(new Vector2Int(chunkX, chunkY), new ChunkData(meshData, textureData, biomes, chunkSize, chunkSize, new List<WorldObjectData>(), bounds));
            }
        });

        // Calculate which chunk each world object belongs to

        // SEQUENTIAL

        Dictionary<Vector2Int, Dictionary<GameObject, List<(Vector3, Vector3)>>> objectsForChunk = new Dictionary<Vector2Int, Dictionary<GameObject, List<(Vector3, Vector3)>>>();

        foreach (TerrainMap.WorldObjectData obj in map.WorldObjects)
        {
            TerrainMapIndexToChunk(chunkSize, new Vector2Int(obj.ClosestIndexX, obj.ClosestIndexY), out Vector2Int chunk, out Vector2Int relative); 

            if (data.TryGetValue(chunk, out ChunkData value))
            {
                if(!objectsForChunk.ContainsKey(chunk))
                {
                    objectsForChunk.Add(chunk, new Dictionary<GameObject, List<(Vector3, Vector3)>>());
                }

                var chunkObjects = objectsForChunk[chunk];

                if(!chunkObjects.ContainsKey(obj.Prefab))
                {
                    chunkObjects.Add(obj.Prefab, new List<(Vector3, Vector3)>());
                }

                var objects = chunkObjects[obj.Prefab];

                // TODO MESH Y

                objects.Add((obj.LocalPosition, obj.Rotation));
            }
            else
            {
                Debug.LogError("SplitIntoChunks: Missing dict chunk for world object");
            }
        }

        foreach(var objects in objectsForChunk)
        {
            if (data.TryGetValue(objects.Key, out ChunkData chunk))
            {
                foreach(var obj in objects.Value)
                {
                    chunk.WorldObjects.Add(new WorldObjectData() { Prefab = obj.Key, WorldPositionsAndRotations = obj.Value });
                }
            }
            else
            {
                Debug.LogError("ERROR");
            }
                
        }

        return data;
    }


    private List<TerrainMap.WorldObjectData> GenerateTerrainMapProceduralObjects(TerrainMap map, float minRadius, int iterations, Vector2 worldBoundsSize)
    {
        System.Random r = new System.Random(CurrentSettings.Seed);
        List<TerrainMap.WorldObjectData> worldObjects = new List<TerrainMap.WorldObjectData>();

        // Get the local positions
        List<Vector2> localPositions = PoissonDiscSampling.GenerateLocalPoints(minRadius, worldBoundsSize, CurrentSettings.Seed, iterations);

        // Loop through each position
        foreach (Vector2 pos in localPositions)
        {
            TerrainSettings.ProceduralObject attempt = TerrainSettings.ProceduralObjects[r.Next(0, TerrainSettings.ProceduralObjects.Count)];

            if (attempt.Do && r.NextDouble() <= attempt.Chance)
            {
                if (Utils.GetClosestIndex(pos, Vector2.zero, worldBoundsSize, map.Width, map.Height, out int x, out int y))
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
                            worldObjects.Add(new TerrainMap.WorldObjectData()
                            {
                                LocalPosition = new Vector3(pos.x, 0, pos.y),
                                Rotation = new Vector3(0, (float)r.NextDouble() * 360, 0),
                                Prefab = attempt.Prefabs[r.Next(0, attempt.Prefabs.Count)],
                                ClosestIndexX = x,
                                ClosestIndexY = y,
                            });
                        }
                    }
                }
                else
                {
                    Debug.LogError("GenerateTerrainMapProceduralObjects: Failed to choose closest terrain map index for position");
                }
            }
        }

        return worldObjects;
    }



#if false

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

#endif

    /// <summary>
    /// 
    /// </summary>
    /// <param name="map"></param>
    /// <param name="checkedFloodFill"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns>A list of the valid points within this course (for start/end pos)</returns>
    private List<Vector2Int> CalculateCourseFloodFillMain(TerrainMap map, ref bool[] checkedFloodFill, int x, int y)
    {
        Queue<(int, int)> q = new Queue<(int, int)>();

        List<Vector2Int> validPoints = new List<Vector2Int>();

        q.Enqueue((x, y));

        // Each element n of Q
        while (q.Count > 0)
        {
            (int, int) pos = q.Dequeue();

            // Check towards the west
            for (int west = pos.Item1; west >= 0 && map.Greens[(pos.Item2 * map.Width) + west]; west--)
            {
                CalculateCourseFloodFillHorizontal(map, ref checkedFloodFill, q, west, pos.Item2, validPoints);
            }

            // Check towards the east
            for (int east = pos.Item1; east < map.Width && map.Greens[(pos.Item2 * map.Width) + east]; east++)
            {
                CalculateCourseFloodFillHorizontal(map, ref checkedFloodFill, q, east, pos.Item2, validPoints);
            }
        }

        return validPoints;
    }

    private void CalculateCourseFloodFillHorizontal(TerrainMap map, ref bool[] checkedFloodFill, Queue<(int, int)> q, int x, int y, List<Vector2Int> validPoints)
    {
        int index = (y * map.Width) + x;

        if (!checkedFloodFill[index])
        {
            checkedFloodFill[index] = true;

            // Add this point if it could be a valid hole
            if (TerrainSettings.ValidHoleBiomes.Contains(map.Biomes[index]))
            {
                validPoints.Add(new Vector2Int(x, y));
            }

            // Check south
            int newY = y + 1;
            if (newY < map.Height && map.Greens[(newY * map.Width) + x] && !q.Contains((x, newY)))
            {
                q.Enqueue((x, newY));
            }

            // And north
            newY = y - 1;
            if (newY >= 0 && map.Greens[(newY * map.Width) + x] && !q.Contains((x, newY)))
            {
                q.Enqueue((x, newY));
            }
        }
    }


#if false

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
        public Bounds Bounds;

        public Biome.Type[] Biomes;
        public int BiomesWidth, BiomesHeight;

        public ChunkData(MeshGenerator.MeshData meshData, TextureGenerator.TextureData textureData, Biome.Type[] biomes, int width, int height, List<WorldObjectData> worldObjects, Bounds bounds)
        {
            MeshData = meshData;
            TextureData = textureData;
            Biomes = biomes;
            BiomesWidth = width;
            BiomesHeight = height;
            WorldObjects = worldObjects;
            Bounds = bounds;
        }
    }

    [Serializable]
    public class GenerationSettings
    {
        public int Seed = 0;
    }

}
