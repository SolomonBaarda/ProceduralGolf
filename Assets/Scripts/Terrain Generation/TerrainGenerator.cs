﻿#define GOLF_PARALLEL_ROUTINES
//#define DEBUG_FLOOD_FILL

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

    private const float WorldObjectYOffset = 0.2f;

    private const int NumAttemptsToChooseRandomCoursePositions = 100;

    public void Generate(GenerationSettings settings, GameManager.CourseGenerated callback)
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

        StartCoroutine(WaitForGenerate(callback));
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

    private IEnumerator WaitForGenerate(GameManager.CourseGenerated callback)
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

        yield return null;

        // TERRAIN LAYERS


        // Construct the terrain map for the whole course
        int size = TerrainSettings.NumChunksToGenerateSize * TerrainSettings.SamplePointFrequency;
        TerrainMap map = new TerrainMap(size, size);

        Vector2 offset = Vector2.zero;
        float distanceBetweenNoiseSamples = TerrainChunkManager.ChunkSizeWorldUnits / (TerrainSettings.SamplePointFrequency - 1);




        GenerateTerrain(map, out float minHeight, out float maxHeight);


        // Use height curve to calculate new height distribution
        AnimationCurve threadSafe = new AnimationCurve(TerrainSettings.HeightDistribution.keys);
        float maxMinusMin = maxHeight - minHeight;


        // Now calculate the final height for the vertex
        For(0, map.Heights.Length, (int index) =>
        {
            // Normalise the height
            map.Heights[index] = (map.Heights[index] - minHeight) / maxMinusMin;

            // Apply the curve
            if (TerrainSettings.UseHeightDistributionCurve)
            {
                map.Heights[index] = threadSafe.Evaluate(map.Heights[index]);
            }

            // And scale by a fixed value
            map.Heights[index] *= TerrainSettings.HeightMultiplier;
        });

        Logger.Log($"* Generated terrain in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;
        yield return null;


        // SEQUENTIAL

        // Now that biomes have been assigned, we can calculate the procedural object positions
        map.WorldObjects = !atLeastOneObject ?
            new List<TerrainMap.WorldObjectData>() :
            GenerateTerrainMapProceduralObjects(map, TerrainSettings.PoissonSamplingRadius, TerrainSettings.PoissonSamplingIterations, new Vector2(map.Width, map.Height) * distanceBetweenNoiseSamples);


        Logger.Log($"* Generated object positions in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;
        yield return null;

        // Now subdivide the data into chunks and calculate the mesh data
        int chunkSize = TerrainSettings.SamplePointFrequency;
        ConcurrentDictionary<Vector2Int, TerrainChunkData> data = SplitIntoChunksAndGenerateMeshData(map, chunkSize, offset, distanceBetweenNoiseSamples);

        // Partially sequential
        List<CourseData> courses = CalculateCourses(map, CurrentSettings.Seed, chunkSize, distanceBetweenNoiseSamples, NumAttemptsToChooseRandomCoursePositions, chunkSize / 8);

        // Create the object and set the data
        TerrainData terrain = ScriptableObject.CreateInstance<TerrainData>();
        terrain.SetData(CurrentSettings.Seed, data.Values.ToList(), courses, TerrainSettings.name);

        Logger.Log($"* Generated chunks and meshes in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;


        string message = $"Finished generating terrain. Completed in {(DateTime.Now - startTimestamp).TotalSeconds:0.0} seconds";
        Logger.Log(message);
        yield return null;

        IsGenerating = false;

        // Callback when done
        callback(terrain);
    }


    static void TerrainMapIndexToChunk(int chunkSize, Vector2Int terrainMapIndex, out Vector2Int chunk, out Vector2Int chunkRelativeIndex)
    {
        int chunkMinusOne = chunkSize - 1;

        chunk = terrainMapIndex / chunkMinusOne;
        chunkRelativeIndex = new Vector2Int(terrainMapIndex.x % chunkMinusOne, terrainMapIndex.y % chunkMinusOne);
    }

    //static bool ChunkRelativeIndexToTerrainMap(int chunkSize, Vector2Int chunk, Vector2Int chunkRelativeIndex, out Vector2Int terrainMapIndex) { }


    /// <summary>
    /// Generate raw noise, normalise it, combine noise layers, and construct green flood fill boolean array
    /// </summary>
    /// <param name="map"></param>
    /// <param name="offset"></param>
    /// <param name="distanceBetweenNoiseSamples"></param>
    private void GenerateTerrain(TerrainMap map, out float minHeight, out float maxHeight)
    {
        for (int i = 0; i < TerrainSettings.TerrainLayers.Count; i++)
        {
            map.Layers.Add(new TerrainMap.Layer(new float[] { }, Biome.Type.None));
        }

        // Get all the noise layers for the terrain
        For(0, TerrainSettings.TerrainLayers.Count, (int index) =>
            {
                TerrainSettings.LayerSettings layerSettings = TerrainSettings.TerrainLayers[index];

                // Only generate the noise if this layer uses it
                //if (!layerSettings.ShareOtherLayerNoise)
                {
                    int seed = CurrentSettings.Seed.GetHashCode() + index.GetHashCode();

                    // Generate the noise for this layer and normalise it
                    float[] noise = Noise.GetNoise(layerSettings.Settings, seed, map.Width, map.Height, out float min, out float max);
                    Noise.NormaliseNoise(ref noise, min, max);

                    map.Layers[index] = new TerrainMap.Layer(noise, layerSettings.Biome);
                }
                //else
                //{
                //    map.Layers.Add(new TerrainMap.Layer(new float[] { }, layerSettings.Biome));
                //}
            });

        // Now calculate the actual heights from the noise and the biomes
        For(0, map.Height, (int y) =>
        {
            List<AnimationCurve> threadSafeCurves = new List<AnimationCurve>();
            for (int i = 0; i < TerrainSettings.TerrainLayers.Count; i++)
            {
                threadSafeCurves.Add(new AnimationCurve(TerrainSettings.TerrainLayers[i].DistanceFromOriginCurve.keys));
            }

            for (int x = 0; x < map.Width; x++)
            {
                int index = (y * map.Width) + x;

                // Set the default biome and height
                map.Biomes[index] = TerrainSettings.MainBiome;
                map.Heights[index] = 0.0f;

                Vector2Int directionToCentre = new Vector2Int(x, y) - new Vector2Int(map.Width / 2, map.Height / 2);
                float distanceFalloffRadius = Math.Min(map.Width, map.Height) / 2.0f;
                // 0 - 1 value 
                float distanceFromCentreScaled = Math.Clamp(directionToCentre.sqrMagnitude / (distanceFalloffRadius * distanceFalloffRadius), 0.0f, 1.0f);

                for (int layerIndex = 0; layerIndex < map.Layers.Count; layerIndex++)
                {
                    TerrainSettings.LayerSettings layerSettings = TerrainSettings.TerrainLayers[layerIndex];
                    TerrainMap.Layer currentLayer = map.Layers[layerIndex];

                    // Set the reference to be another layer if we are sharing noise
                    if (layerSettings.ShareOtherLayerNoise)
                    {
                        currentLayer = map.Layers[layerSettings.LayerIndexShareNoise];
                    }

                    // Scale noise according to the distance from the origin
                    if (layerSettings.UseDistanceFromOriginCurve)
                    {
                        currentLayer.Noise[index] *= threadSafeCurves[layerIndex].Evaluate(distanceFromCentreScaled);
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
                                TerrainSettings.LayerSettings mask = TerrainSettings.TerrainLayers[layerSettings.Masks[layerMaskIndex].LayerIndex];
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

                            float value = currentLayer.Noise[index] * layerSettings.Multiplier;

                            switch (layerSettings.CombinationMode)
                            {
                                case TerrainSettings.LayerSettings.Mode.Add:
                                    map.Heights[index] += value;
                                    break;
                                case TerrainSettings.LayerSettings.Mode.Subtract:
                                    map.Heights[index] -= value;
                                    break;
                                case TerrainSettings.LayerSettings.Mode.Divide:
                                    map.Heights[index] /= value;
                                    break;
                                case TerrainSettings.LayerSettings.Mode.Multiply:
                                    map.Heights[index] *= value;
                                    break;
                                case TerrainSettings.LayerSettings.Mode.Modulus:
                                    map.Heights[index] %= value;
                                    break;
                                case TerrainSettings.LayerSettings.Mode.Set:
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
                foreach (TerrainSettings.CourseSettings g in TerrainSettings.Course)
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

                                TerrainSettings.LayerSettings layerSettings = TerrainSettings.TerrainLayers[greenLayerMask.LayerIndex];

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
            }
        });

        // TODO PARALLELISE:

        minHeight = map.Heights[0];
        maxHeight = minHeight;

        foreach (float height in map.Heights)
        {
            if (height < minHeight)
            {
                minHeight = height;
            }

            if (height > maxHeight)
            {
                maxHeight = height;
            }
        }
    }


    /// <summary>
    /// Calculate start/hole positions for each golf course
    /// </summary>
    /// <param name="map"></param>
    /// <param name="greens"></param>
    private List<CourseData> CalculateCourses(TerrainMap map, int seed, int chunkSize, float distanceBetweenNoiseSamples, int numAttemptsToChooseRandomPositions = 100, int precisionStep = 1)
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
                    FloodFillGolfCourse(map, ref checkedFloodFill, x, y, out List<Vector2Int> possibleCoursePoints);

                    // Ensure only valid courses get added
                    if (possibleCoursePoints.Count > 2 && possibleCoursePoints.Count > TerrainSettings.FairwayMinStartEndVertexCount)
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

        ConcurrentBag<Tuple<Vector3, Vector3>> startFinishCourses = new ConcurrentBag<Tuple<Vector3, Vector3>>();

        // Calculate the start and end positions for each of those courses
        For(0, coursePoints.Count, (int index) =>
        {
            List<Vector2Int> points = coursePoints[index];

            // Find furthest away points for start and finish
            int curSqrMag = 0;
            Vector2Int start = points[0];
            Vector2Int hole = points[1];
            System.Random r = new System.Random(seed + index);

            // Do a certain number of random attempts
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

            // Calculate the mesh position
            Vector3 startWorld = CalculateWorldVertexPositionFromTerrainMapIndex(map, start.x, start.y, distanceBetweenNoiseSamples);
            Vector3 holeWorld = CalculateWorldVertexPositionFromTerrainMapIndex(map, hole.x, hole.y, distanceBetweenNoiseSamples);

            // Ensure that the holes are far enough away for a decent course
            Vector2 distance = new Vector2(holeWorld.x - startWorld.x, holeWorld.z - startWorld.z);
            if (distance.sqrMagnitude > TerrainSettings.MinimumWorldDistanceBetweenHoles * TerrainSettings.MinimumWorldDistanceBetweenHoles)
            {
                // Create the course data object
                startFinishCourses.Add(new(startWorld, holeWorld));
            }
        });


        List<CourseData> courses = new List<CourseData>();
        System.Random r = new System.Random(0);

        foreach (var startEnd in startFinishCourses)
        {
            UnityEngine.Color c = new UnityEngine.Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
            courses.Add(new CourseData(startEnd.Item1, startEnd.Item2, c));
        }

        return courses;
    }

    private static Vector3 CalculateWorldVertexPositionFromTerrainMapIndex(TerrainMap map, int x, int y, float distanceBetweenNoiseSamples)
    {
        return new Vector3(x * distanceBetweenNoiseSamples, map.Heights[(y * map.Width) + x], y * distanceBetweenNoiseSamples);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="map"></param>
    private ConcurrentDictionary<Vector2Int, TerrainChunkData> SplitIntoChunksAndGenerateMeshData(TerrainMap map, int chunkSize, Vector2 offset, float distanceBetweenNoiseSamples)
    {
        ConcurrentDictionary<Vector2Int, TerrainChunkData> chunkData = new ConcurrentDictionary<Vector2Int, TerrainChunkData>();

        int numChunksY = map.Height / chunkSize, numChunksX = map.Width / chunkSize;
        int increment = MeshSettings.SimplificationIncrement;
        int newChunkSize = ((chunkSize - 1) / increment) + 1;

        int numVertexesPerChunk = newChunkSize * newChunkSize;
        int numTrianglesPerChunk = (newChunkSize - 1) * (newChunkSize - 1) * 6;

        UInt32 chunkSizeLODUnsigned = Convert.ToUInt32(newChunkSize);

        var writableMeshData = Mesh.AllocateWritableMeshData(numChunksY * numChunksX);
        Mesh[] meshes = new Mesh[numChunksY * numChunksX];
        int[] meshIDs = new int[numChunksY * numChunksX];

        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i] = new Mesh();
            meshIDs[i] = meshes[i].GetInstanceID();
        }


        For(0, numChunksY, (int chunkY) =>
        {
            for (int chunkX = 0; chunkX < numChunksX; chunkX++)
            {
                // Calculate chunk bounds
                Vector3 centre = new Vector3((distanceBetweenNoiseSamples * (chunkSize - 1) * chunkX) + offset.x, 0, (distanceBetweenNoiseSamples * (chunkSize - 1) * chunkY) + offset.y);
                Bounds bounds = new Bounds(centre, new Vector3(TerrainChunkManager.ChunkSizeWorldUnits, 0, TerrainChunkManager.ChunkSizeWorldUnits));

                // Initialise the new mesh data
                int meshIndex = (chunkY * numChunksY) + chunkX;
                //meshes[meshIndex].name = $"Chunk({chunkX},{chunkY})";
                var data = writableMeshData[meshIndex];

                Biome.Type[] biomes = new Biome.Type[numVertexesPerChunk];

                // Create the buffers
                data.SetVertexBufferParams(numVertexesPerChunk,
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0),
                    new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, dimension: 4, stream: 1)
                );

                data.SetIndexBufferParams(numTrianglesPerChunk, IndexFormat.UInt32);


                // Get references to the buffers
                var positions = data.GetVertexData<Vector3>(stream: 0);
                var colours = data.GetVertexData<Color32>(stream: 1);
                var triangles = data.GetIndexData<UInt32>();


                // Set the data in the buffers
                int triangleIndex = 0;
                for (int y = 0; y < chunkSize; y += increment)
                {
                    for (int x = 0; x < chunkSize; x += increment)
                    {
                        int newX = x / increment, newY = y / increment;

                        int vertexIndex = (newY * newChunkSize) + newX;
                        int terrainMapX = (chunkX * chunkSize) - chunkX + newX;
                        int terrainMapY = (chunkY * chunkSize) - chunkY + newY;

                        // Set the biomes
                        biomes[vertexIndex] = map.Biomes[(terrainMapY * map.Width) + terrainMapX];

                        // Set the vertex
                        positions[vertexIndex] = CalculateWorldVertexPositionFromTerrainMapIndex(map, terrainMapX, terrainMapY, distanceBetweenNoiseSamples);

                        // Set the colour
                        colours[vertexIndex] = TextureSettings.GetColour(biomes[vertexIndex]);

                        if (newX >= 0 && newX < newChunkSize - 1 && newY >= 0 && newY < newChunkSize - 1)
                        {
                            UInt32 vertexIndexUnsigned = Convert.ToUInt32(vertexIndex);

                            // Set the triangles
                            triangles[triangleIndex] = vertexIndexUnsigned;
                            // Below
                            triangles[triangleIndex + 1] = vertexIndexUnsigned + chunkSizeLODUnsigned;
                            // Bottom right
                            triangles[triangleIndex + 2] = vertexIndexUnsigned + chunkSizeLODUnsigned + 1U;
                            triangleIndex += 3;

                            triangles[triangleIndex] = vertexIndexUnsigned;
                            // Bottom right
                            triangles[triangleIndex + 1] = vertexIndexUnsigned + chunkSizeLODUnsigned + 1U;
                            // Top right
                            triangles[triangleIndex + 2] = vertexIndexUnsigned + 1U;
                            triangleIndex += 3;
                        }
                    }
                }



#if false

                // Calculate UVs
                // Now not needed

                // Get the minimum and maximum points
                Vector2 min = Vertices[0], max = Vertices[0];
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Vector3 v = Vertices[(y * Width) + x];

                        if (v.x < min.x) { min.x = v.x; }
                        if (v.x > max.x) { max.x = v.x; }

                        if (v.z < min.y) { min.y = v.z; }
                        if (v.z > max.y) { max.y = v.z; }
                    }
                }

                Vector2 size = max - min;

                // Now assign each UV
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Vector3 point = Vertices[(y * Width) + x];

                        UVs[(y * Width) + x] = (max - new Vector2(point.x, point.z)) / size;
                    }
                }

#endif

                // Now set the sub mesh
                data.subMeshCount = 1;
                data.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles));

                Vector2Int chunk = new Vector2Int(chunkX, chunkY);
                chunkData.TryAdd(chunk, new TerrainChunkData(chunk, bounds, biomes, newChunkSize, newChunkSize, meshes[meshIndex], new List<WorldObjectData>()));
            }
        });

        // Now apply the meshdata to the meshes and clean up the memory
        Mesh.ApplyAndDisposeWritableMeshData(writableMeshData, meshes);

        // Recalculate mesh values (SEQUENTIAL)
        foreach (Mesh mesh in meshes)
        {
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            // Optimise it
            mesh.Optimize();
        }

        // Bake the mesh collision data in parallel 
        ForEach(meshIDs, (id) =>
        {
            Physics.BakeMesh(id, false);
        });


        // Calculate which chunk each world object belongs to

        // SEQUENTIAL

        Dictionary<Vector2Int, Dictionary<GameObject, List<(Vector3, Vector3)>>> objectsForChunk = new Dictionary<Vector2Int, Dictionary<GameObject, List<(Vector3, Vector3)>>>();

        // TODO BETTER PERFORMANCE:
        foreach (TerrainMap.WorldObjectData obj in map.WorldObjects)
        {
            TerrainMapIndexToChunk(chunkSize, new Vector2Int(obj.ClosestIndexX, obj.ClosestIndexY), out Vector2Int chunk, out Vector2Int _);

            if (chunkData.TryGetValue(chunk, out TerrainChunkData value))
            {
                if (!objectsForChunk.ContainsKey(chunk))
                {
                    objectsForChunk.Add(chunk, new Dictionary<GameObject, List<(Vector3, Vector3)>>());
                }

                var chunkObjects = objectsForChunk[chunk];

                if (!chunkObjects.ContainsKey(obj.Prefab))
                {
                    chunkObjects.Add(obj.Prefab, new List<(Vector3, Vector3)>());
                }

                var objects = chunkObjects[obj.Prefab];

                objects.Add((obj.LocalPosition, obj.Rotation));
            }
            else
            {
                Debug.LogError("SplitIntoChunks: Missing dict chunk for world object");
            }
        }

        foreach (var objects in objectsForChunk)
        {
            if (chunkData.TryGetValue(objects.Key, out TerrainChunkData chunk))
            {
                foreach (var obj in objects.Value)
                {
                    chunk.WorldObjects.Add(new WorldObjectData() { Prefab = obj.Key, WorldPositionsAndRotations = obj.Value });
                }
            }
            else
            {
                Debug.LogError("ERROR");
            }

        }

        return chunkData;
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
            TerrainSettings.ObjectSettings attempt = TerrainSettings.ProceduralObjects[r.Next(0, TerrainSettings.ProceduralObjects.Count)];

            if (attempt.Do && r.NextDouble() <= attempt.Chance)
            {
                if (Utils.GetClosestIndex(pos, Vector2.zero, worldBoundsSize, map.Width, map.Height, out int x, out int y))
                {
                    int terrainMapIndex = (y * map.Width) + x;
                    Biome.Type biome = map.Biomes[terrainMapIndex];

                    // The biome for this position is valid
                    if (attempt.RequiredBiomes.Contains(biome))
                    {
                        // Check that the mask is valid if we are using it
                        bool maskvalid = true;
                        if (attempt.UseMask)
                        {
                            for (int j = 0; j < attempt.Masks.Count; j++)
                            {
                                TerrainMap.Layer maskValues = map.Layers[attempt.Masks[j].LayerIndex];
                                // Mask is not valid here
                                if (!(maskValues.Noise[terrainMapIndex] >= attempt.Masks[j].NoiseThresholdMin &&
                                    maskValues.Noise[terrainMapIndex] <= attempt.Masks[j].NoiseThresholdMax))
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
                                LocalPosition = new Vector3(pos.x, map.Heights[terrainMapIndex] - WorldObjectYOffset, pos.y),
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
    /// <param name="validStartEndPositions"></param>
    private void FloodFillGolfCourse(TerrainMap map, ref bool[] checkedFloodFill, int x, int y, out List<Vector2Int> validStartEndPositions)
    {
        Queue<(int, int)> q = new Queue<(int, int)>();

        validStartEndPositions = new List<Vector2Int>();

        q.Enqueue((x, y));

        // Each element n of Q
        while (q.Count > 0)
        {
            (int, int) pos = q.Dequeue();

            // Check towards the west
            for (int west = pos.Item1; west >= 0 && map.Greens[(pos.Item2 * map.Width) + west]; west--)
            {
                FloodFillGolfCoursePosition(map, ref checkedFloodFill, q, west, pos.Item2, ref validStartEndPositions);
            }

            // Check towards the east
            for (int east = pos.Item1; east < map.Width && map.Greens[(pos.Item2 * map.Width) + east]; east++)
            {
                FloodFillGolfCoursePosition(map, ref checkedFloodFill, q, east, pos.Item2, ref validStartEndPositions);
            }
        }
    }

    private bool AllMasksValidForPosition(TerrainMap map, IEnumerable<TerrainSettings.Mask> masks, int x, int y)
    {
        int index = (y * map.Width) + x;

        foreach (TerrainSettings.Mask mask in masks)
        {
            TerrainSettings.LayerSettings layerSettings = TerrainSettings.TerrainLayers[mask.LayerIndex];

            // Get the reference to the noise layer that we are comparing against
            TerrainMap.Layer noiseLayer = layerSettings.ShareOtherLayerNoise ?
                map.Layers[layerSettings.LayerIndexShareNoise] :
                map.Layers[mask.LayerIndex];

            // Check if the mask is valid
            if (!(noiseLayer.Noise[index] >= mask.NoiseThresholdMin && noiseLayer.Noise[index] <= mask.NoiseThresholdMax))
            {
                return false;
            }
        }

        return true;

    }

    private void FloodFillGolfCoursePosition(TerrainMap map, ref bool[] checkedFloodFill, Queue<(int, int)> q, int x, int y, ref List<Vector2Int> validPoints)
    {
        int index = (y * map.Width) + x;

        if (!checkedFloodFill[index])
        {
            checkedFloodFill[index] = true;

            // Add this point if it could be a valid hole
            foreach (var holeSettings in TerrainSettings.Holes)
            {
                if (holeSettings.Do && holeSettings.RequiredBiomes.Contains(map.Biomes[index]) && AllMasksValidForPosition(map, holeSettings.Masks, x, y))
                {
                    validPoints.Add(new Vector2Int(x, y));
                    break;
                }
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


    [Serializable]
    public class GenerationSettings
    {
        public int Seed = 0;
    }

}
