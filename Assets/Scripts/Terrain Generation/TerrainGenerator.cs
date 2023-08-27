#define GOLF_PARALLEL_ROUTINES
//#define UnityEngine.Debug_FLOOD_FILL

using C5;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainGenerator : MonoBehaviour
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

    private const float WorldObjectYOffset = 0.05f;

    private const int NumAttemptsToChooseRandomCoursePositions = 100;

    public delegate void OnCourseGenerated(TerrainData data);

    public delegate void PreviewGenerated(Texture2D map);

    public void Generate(GenerationSettings settings, OnCourseGenerated callback)
    {
        if (IsGenerating)
        {
            UnityEngine.Debug.LogError("Terrain is already being generated");
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
            UnityEngine.Debug.LogError("No procedural objects have been added");

        CurrentSettings = settings;

        StartCoroutine(WaitForGenerate(atLeastOneObject, callback));
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

    private IEnumerator WaitForGenerate(bool atLeastOneObject, OnCourseGenerated callback)
    {
        UnityEngine.Debug.Log($"Starting generation using seed {CurrentSettings.Seed}");

        DateTime startTimestamp = DateTime.Now, lastTimestamp = startTimestamp;
        IsGenerating = true;

        // TERRAIN LAYERS

        // Construct the terrain map for the whole course
        int size = TerrainSettings.NumChunksToGenerateSize * (TerrainSettings.SamplePointFrequency - 1) + 1;
        TerrainMap map = new TerrainMap(size, size);

        float distanceBetweenNoiseSamples = TerrainChunkData.ChunkSizeWorldUnits / (TerrainSettings.SamplePointFrequency - 1);

        GenerateTerrain(map, out float waterHeight);

        UnityEngine.Debug.Log($"Generated terrain in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;
        yield return null;


        // SEQUENTIAL
        // Now that biomes have been assigned, we can calculate the procedural object positions
        map.WorldObjects = !atLeastOneObject ?
            new List<TerrainMap.WorldObjectData>() :
            GenerateTerrainMapProceduralObjects(map, TerrainSettings.PoissonSamplingRadius, TerrainSettings.PoissonSamplingIterations, new Vector2(map.Width, map.Height) * distanceBetweenNoiseSamples);

        UnityEngine.Debug.Log($"Generated object positions in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;
        yield return null;


        // Partially sequential
        int chunkSize = TerrainSettings.SamplePointFrequency;
        List<CourseData> courses = CalculateCourses(map, CurrentSettings.Seed, chunkSize, distanceBetweenNoiseSamples, NumAttemptsToChooseRandomCoursePositions, chunkSize / 8);

        // Sequential but short
        var invalidBiomes = CalculateInvalidBiomesForCourse();

        UnityEngine.Debug.Log($"Generated courses in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;
        yield return null;


        // Now subdivide the data into chunks and calculate the mesh data
        ConcurrentDictionary<Vector2Int, TerrainChunkData> data = SplitIntoChunksAndGenerateMeshData(map, chunkSize, distanceBetweenNoiseSamples);

        Mesh waterMesh = TerrainSettings.DoWater ? GenerateWaterMesh(map, waterHeight, distanceBetweenNoiseSamples) : null;

        UnityEngine.Debug.Log($"Generated chunks and meshes in {(DateTime.Now - lastTimestamp).TotalSeconds:0.0} seconds\"");
        lastTimestamp = DateTime.Now;
        yield return null;


        // Create the object and set the data
        TerrainData terrain = new TerrainData(CurrentSettings.Seed, data.Values.ToList(), courses, TerrainSettings.DoWater, waterMesh, waterHeight, map.Width, invalidBiomes, TextureSettings.GetColour(TerrainSettings.BackgroundBiome), TerrainSettings.name);


        string message = $"Finished generating terrain. Completed in {(DateTime.Now - startTimestamp).TotalSeconds:0.0} seconds";
        UnityEngine.Debug.Log(message);
        yield return null;


        IsGenerating = false;

        // Callback when done
        callback(terrain);
    }

    private System.Collections.Generic.HashSet<Biome.Type> CalculateInvalidBiomesForCourse()
    {
        // Calculate which biomes are allowed
        // Always allow none as otherwise we couldn't shoot the ball
        var allowedBiomes = new System.Collections.Generic.HashSet<Biome.Type>() { Biome.Type.None };
        foreach (var setting in TerrainSettings.Course)
        {
            foreach (var biome in setting.RequiredBiomes)
            {
                allowedBiomes.Add(biome);
            }
        }

        return Biome.GetAllBiomes().Except(allowedBiomes).ToHashSet();
    }


    static void TerrainMapIndexToChunk(int chunkSize, Vector2Int terrainMapIndex, out Vector2Int chunk, out Vector2Int chunkRelativeIndex)
    {
        chunk = terrainMapIndex / chunkSize;
        chunkRelativeIndex = new Vector2Int(terrainMapIndex.x % chunkSize, terrainMapIndex.y % chunkSize);
    }


    /// <summary>
    /// Generate raw noise, normalise it, combine noise layers, and construct green flood fill boolean array
    /// </summary>
    /// <param name="map"></param>
    /// <param name="offset"></param>
    /// <param name="distanceBetweenNoiseSamples"></param>
    private void GenerateTerrain(TerrainMap map, out float waterHeight)
    {
        for (int i = 0; i < TerrainSettings.TerrainLayers.Count; i++)
        {
            map.Layers.Add(new TerrainMap.Layer(new float[] { }, Biome.Type.None));
        }

        var minMax = new (float, float)[TerrainSettings.TerrainLayers.Count];

        // Get all the noise layers for the terrain
        For(0, TerrainSettings.TerrainLayers.Count, (int layerIndex) =>
            {
                TerrainSettings.LayerSettings layerSettings = TerrainSettings.TerrainLayers[layerIndex];

                // Only generate the noise if this layer uses it
                if (layerSettings.ShareOtherLayerNoise)
                {
                    map.Layers[layerIndex] = new TerrainMap.Layer(new float[] { }, layerSettings.Biome);
                    return;
                }

                int seed = CurrentSettings.Seed.GetHashCode() + layerIndex.GetHashCode();

                // Generate the noise for this layer and normalise it
                float[] noise = Noise.GetNoise(layerSettings.Settings, seed, map.Width, map.Height, out float min, out float max);

                // Record the min and max
                minMax[layerIndex] = new (min, max);

                map.Layers[layerIndex] = new TerrainMap.Layer(noise, layerSettings.Biome);
            });


        float distanceFalloffRadius = Math.Min(map.Width, map.Height) / 2.0f;
        var centre = new Vector2Int(map.Width / 2, map.Height / 2);

        // Loop over each layer
        for (int layerIndex = 0; layerIndex < TerrainSettings.TerrainLayers.Count; layerIndex++)
        {
            if (TerrainSettings.TerrainLayers[layerIndex].ShareOtherLayerNoise) continue;

            float min = minMax[layerIndex].Item1;
            float max = minMax[layerIndex].Item2;
            float maxMinusMin = max - min;

            // Process the layer in parallel
            For(0, map.Height, (int y) =>
            {
                // Make thread local copies of the animation curve as sharing these objects blocks access
                var threadSafeDistanceOriginCurve = new AnimationCurve(TerrainSettings.TerrainLayers[layerIndex].DistanceFromOriginCurve.keys);

                for (int x = 0; x < map.Width; x++)
                {
                    int noiseIndex = y * map.Width + x;

                    // Normalise the sample
                    map.Layers[layerIndex].Noise[noiseIndex] = (map.Layers[layerIndex].Noise[noiseIndex] - min) / maxMinusMin;

                    // Scale noise according to the distance from the origin
                    if (TerrainSettings.TerrainLayers[layerIndex].UseDistanceFromOriginCurve)
                    {
                        Vector2Int directionToCentre = centre - new Vector2Int(x, y);
                        // 0 - 1 value 
                        float distanceFromCentreScaled = Math.Clamp(directionToCentre.magnitude / (distanceFalloffRadius), 0.0f, 1.0f);

                        // Scale the raw noise by the distance from origin curve
                        float multiplier = threadSafeDistanceOriginCurve.Evaluate(distanceFromCentreScaled);

                        map.Layers[layerIndex].Noise[noiseIndex] = Mathf.Clamp01(map.Layers[layerIndex].Noise[noiseIndex] * multiplier);
                    }
                }
            });
        }


        // Calculate the height of the water layer
        waterHeight = TerrainSettings.WaterHeight * TerrainSettings.HeightMultiplier;


        // Now calculate the actual heights from the noise and the biomes
        For(0, map.Height, (int y) =>
        {
            for (int x = 0; x < map.Width; x++)
            {
                int index = (y * map.Width) + x;

                // Set the default biome and height
                map.Biomes[index] = TerrainSettings.BackgroundBiome;
                map.Heights[index] = TerrainSettings.BaseHeight;

                for (int layerIndex = 0; layerIndex < map.Layers.Count; layerIndex++)
                {
                    TerrainSettings.LayerSettings layerSettings = TerrainSettings.TerrainLayers[layerIndex];
                    TerrainMap.Layer currentLayer = map.Layers[layerIndex];

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

                            float sample = layerSettings.OneMinus ? 1.0f - currentLayer.Noise[index] : currentLayer.Noise[index];
                            float value = (sample + layerSettings.Offset) * layerSettings.Multiplier;

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
                                case TerrainSettings.LayerSettings.Mode.Pow:
                                    map.Heights[index] = Mathf.Pow(map.Heights[index], value);
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


                // Check if this point is underwater
                if (TerrainSettings.DoWater && map.Heights[index] <= TerrainSettings.WaterHeight)
                {
                    map.Biomes[index] = TerrainSettings.UnderwaterBiome;
                }


                // Now calculate the final height for the vertex
                // And scale by a fixed value
                map.Heights[index] *= TerrainSettings.HeightMultiplier;








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
    }




    private class Node : IComparable
    {
        public Vector2Int Position;

        /// <summary>
        /// Distance between the current node and the start node
        /// </summary>
        public int Distance;
        /// <summary>
        /// Estimated distance to the end
        /// </summary>
        public int Heuristic;

        public Node Parent;

        public Node(Vector2Int position, int distance, int heuristic, Node parent)
        {
            Position = position;
            Distance = distance;
            Heuristic = heuristic;
            Parent = parent;
        }

        public int Cost => Distance + Heuristic;

        public override bool Equals(object obj)
        {
            return obj is Node node && Position.Equals(node.Position);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position);
        }

        public int CompareTo(object obj)
        {
            return Cost.CompareTo((obj as Node).Cost);
        }
    }

    private static List<Vector2Int> CalculateShortestPathOnCourseFromStartToEnd(TerrainMap map, Vector2Int start, Vector2Int end)
    {
        // Structure for efficiently prioritising nodes
        // Interval heap is required for replacing already open nodes with the shorter path
        var openList = new IntervalHeap<Node>();

        // Structures for quickly checking if a node is open or closed
        var openNodeHandles = new Dictionary<Vector2Int, IPriorityQueueHandle<Node>>();
        var closedNodes = new System.Collections.Generic.HashSet<Vector2Int>();


        void AddNodeToOpenList(Node n)
        {
            IPriorityQueueHandle<Node> handle = null;
            openList.Add(ref handle, n);
            openNodeHandles.Add(n.Position, handle);
        }

        // Add the initial node
        var initial = new Node(end, 0, (start - end).sqrMagnitude, null);
        AddNodeToOpenList(initial);


        // Main loop
        while (openList.Count > 0)
        {
            // Get the current node with the lowest cost
            var currentNode = openList.DeleteMin();
            openNodeHandles.Remove(currentNode.Position);
            closedNodes.Add(currentNode.Position);


            // We have found the goal
            if (start.Equals(currentNode.Position))
            {
                // Backtrack from here to get the path
                var path = new List<Vector2Int>();

                Node c = currentNode;
                while (c != null)
                {
                    path.Add(c.Position);
                    c = c.Parent;
                }

                return path;
            }


            List<Node> children = new List<Node>();

            void AddIfValidChild(Vector2Int direction)
            {
                Vector2Int child = currentNode.Position + direction;

                // Ensure valid position
                if (child.x >= 0 && child.y >= 0 && child.x < map.Width && child.y < map.Height && 
                    map.Greens[(child.y * map.Width) + child.x])
                {
                    // Distance travelled
                    int newDistance = currentNode.Distance + 1;

                    // Estimation of the distance to the end node
                    int heuristic = (start - child).sqrMagnitude;

                    children.Add(new Node(child, newDistance, heuristic, currentNode));
                }
            }


            // Add all possible children
            AddIfValidChild(Vector2Int.up);
            AddIfValidChild(Vector2Int.down);
            AddIfValidChild(Vector2Int.left);
            AddIfValidChild(Vector2Int.right);

            // Allow diagonal movement
            AddIfValidChild(new Vector2Int(1, 1));
            AddIfValidChild(new Vector2Int(-1, 1));
            AddIfValidChild(new Vector2Int(1, -1));
            AddIfValidChild(new Vector2Int(-1, -1));

            
            // Check each child
            foreach (var newChild in children)
            {
                // Skip child if it has already been visited
                if (closedNodes.Contains(newChild.Position)) continue;

                
                // See if the node is already in the open list
                if (openNodeHandles.TryGetValue(newChild.Position, out var existingHandle) && openList.Find(existingHandle, out var existing))
                {
                    // We have a new shortest path
                    if (newChild.Distance < existing.Distance)
                    {
                        // Handles remain the same

                        // Replace the existing node with the new node
                        openList.Replace(existingHandle, newChild);
                    }
                }
                else
                {
                    // If this is a new node then add it to the open list
                    AddNodeToOpenList(newChild);
                }
            }
        }

        UnityEngine.Debug.LogError("Failed to find shortest path for course");
        return new List<Vector2Int>() { start, end };
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
        File.WriteAllBytes(Application.dataPath + "/UnityEngine.Debug_floodfill.png", png);

        UnityEngine.Debug.LogWarning($"Wrote flood fill UnityEngine.Debug image to \"{Application.dataPath + "/UnityEngine.Debug_floodfill.png"}\"");

#endif

        ConcurrentBag<Tuple<Vector3, Vector3, List<Vector3>>> startFinishPathCourses = new ConcurrentBag<Tuple<Vector3, Vector3, List<Vector3>>>();

        // Sort courses by size for consistency
        coursePoints.Sort((x, y) => x.Count.CompareTo(y.Count));

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
                // Flatten the hole
                if(TerrainSettings.FlattenStartAndHoleUsingMaxHeight)
                {
                    void FlattenPosition(int posX, int posY, int radius)
                    {
                        int radiusSquared = radius * radius;

                        int minX = Mathf.Clamp(posX - radius, 0, map.Width - 1);
                        int maxX = Mathf.Clamp(posX + radius, 0, map.Width - 1);
                        int minY = Mathf.Clamp(posY - radius, 0, map.Height - 1);
                        int maxY = Mathf.Clamp(posY + radius, 0, map.Height - 1);

                        float maxHeight = map.Heights[posY * map.Width + posX];
                        int numHeightsSampled = 0;

                        for (int y = minY; y <= maxY; y++)
                        {
                            for (int x = minX; x <= maxX; x++)
                            {
                                int i = y * map.Width + x;
                                int newRadiusSquared = ((y - posY) * (y - posY)) + ((x - posX) * (x - posX));

                                if (map.Greens[i] && newRadiusSquared <= radiusSquared)
                                {
                                    if(map.Heights[i] > maxHeight)
                                    {
                                        maxHeight = map.Heights[i];
                                    }
                                    numHeightsSampled++;
                                }
                            }
                        }

                        maxHeight += TerrainSettings.AbsoluteHeightToRaiseFlattenedArea;

                        for (int y = minY; y <= maxY; y++)
                        {
                            for (int x = minX; x <= maxX; x++)
                            {
                                int i = y * map.Width + x;
                                int newRadiusSquared = ((y - posY) * (y - posY)) + ((x - posX) * (x - posX));

                                if (map.Greens[i] && newRadiusSquared <= radiusSquared)
                                {
                                    map.Heights[i] = maxHeight;
                                }
                            }
                        }
                    }

                    // Flatten the start and end positions
                    // Use the same radius for each
                    int radius = r.Next(TerrainSettings.MinDistanceRadiusToFlatten, TerrainSettings.MaxDistanceRadiusToFlatten);
                    FlattenPosition(start.x, start.y, radius);
                    FlattenPosition(hole.x, hole.y, radius);
                }

                var pathPoints = CalculateShortestPathOnCourseFromStartToEnd(map, start, hole);

                var indexesToKeep = new List<int>();

                // Simplify the 2D line
                LineUtility.Simplify(pathPoints.Select(x => new Vector2(x.x, x.y)).ToList(), TerrainSettings.CourseCameraPathSimplificationStrength, indexesToKeep);

                var worldPoints = new List<Vector3>();

                // Map the 2D line to 3D
                foreach (int indexToKeep in indexesToKeep)
                {
                    worldPoints.Add(CalculateWorldVertexPositionFromTerrainMapIndex(map, pathPoints[indexToKeep].x, pathPoints[indexToKeep].y, distanceBetweenNoiseSamples));
                }

                // Create the course data object
                startFinishPathCourses.Add(new(startWorld, holeWorld, worldPoints));
            }
        });


        List<CourseData> courses = new List<CourseData>();
        System.Random r = new System.Random(0);

        foreach (var startEndMid in startFinishPathCourses)
        {
            UnityEngine.Color c = new UnityEngine.Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
            courses.Add(new CourseData(startEndMid.Item1, startEndMid.Item2, startEndMid.Item3, c));
        }

        // Sort by distance to the origin
        courses.Sort((x, y) => x.Start.sqrMagnitude.CompareTo(y.Start.sqrMagnitude));

        return courses;
    }

    private static Vector3 CalculateWorldVertexPositionFromTerrainMapIndex(TerrainMap map, int x, int y, float distanceBetweenNoiseSamples)
    {
        return new Vector3(x * distanceBetweenNoiseSamples, map.Heights[(y * map.Width) + x], y * distanceBetweenNoiseSamples);
    }

    private Mesh GenerateWaterMesh(TerrainMap map, float waterHeight, float distanceBetweenNoiseSamples)
    {
        Vector3 min = CalculateWorldVertexPositionFromTerrainMapIndex(map, 0, 0, distanceBetweenNoiseSamples);
        Vector3 max = CalculateWorldVertexPositionFromTerrainMapIndex(map, map.Width - 1, map.Height - 1, distanceBetweenNoiseSamples);

        Mesh m = new Mesh()
        {
            vertices = new Vector3[] 
            {
                // For some reason setting water height here does nothing
                new Vector3(min.x, 0, min.z), 
                new Vector3(min.x, 0, max.z), 
                new Vector3(max.x, 0, min.z), 
                new Vector3(max.x, 0, max.z) 
            },
            triangles = new int[] 
            { 
                0, 1, 2, 
                1, 3, 2 
            },
            normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
            uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 0), new Vector2(1, 1) },
        };
        
        m.RecalculateTangents();
        m.RecalculateBounds();
        m.Optimize();

        return m;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="map"></param>
    private ConcurrentDictionary<Vector2Int, TerrainChunkData> SplitIntoChunksAndGenerateMeshData(TerrainMap map, int chunkSize, float distanceBetweenNoiseSamples)
    {
        ConcurrentDictionary<Vector2Int, TerrainChunkData> chunkData = new ConcurrentDictionary<Vector2Int, TerrainChunkData>();

        int numChunksY = map.Height / (chunkSize - 1), numChunksX = map.Width / (chunkSize - 1);

        var writableMeshData = Mesh.AllocateWritableMeshData(numChunksY * numChunksX * MeshSettings.LevelOfDetail.Count);
        Mesh[] meshes = new Mesh[numChunksY * numChunksX * MeshSettings.LevelOfDetail.Count];

        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i] = new Mesh();
        }

        // Inline method which returns a list of mesh pointers for the current chunk
        List<Mesh> GenerateLODsForChunk(int chunkX, int chunkY, in Biome.Type[] biomes)
        {
            int baseMeshIndex = ((chunkY * numChunksY) + chunkX) * MeshSettings.LevelOfDetail.Count;
            List<Mesh> LODs = new List<Mesh>();

            for (int LODIndex = 0; LODIndex < MeshSettings.LevelOfDetail.Count; LODIndex++)
            {
                int simplificationIncrement = Mathf.Max(MeshSettings.LevelOfDetail[LODIndex] * 2, 1);

                int newChunkSize = ((chunkSize - 1) / simplificationIncrement) + 1;
                UInt32 chunkSizeU = Convert.ToUInt32(newChunkSize);

                // Initialise the new mesh data
                var data = writableMeshData[baseMeshIndex + LODIndex];

                // Create the buffers
                data.SetVertexBufferParams(newChunkSize * newChunkSize, // Num vertexes
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension:2, stream: 1),
                    new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, dimension: 4, stream: 2)
                );

                data.SetIndexBufferParams(
                    (newChunkSize - 1) * (newChunkSize - 1) * 6, // Num triangles
                    IndexFormat.UInt32
                );

                // Get references to the buffers
                var positions = data.GetVertexData<Vector3>(stream: 0);
                var UVs = data.GetVertexData<Vector2>(stream: 1);
                var colours = data.GetVertexData<Color32>(stream: 2);
                var triangles = data.GetIndexData<UInt32>();


                // Set the data in the buffers
                int triangleIndex = 0;
                for (int y = 0; y < chunkSize; y += simplificationIncrement)
                {
                    for (int x = 0; x < chunkSize; x += simplificationIncrement)
                    {
                        int newX = x / simplificationIncrement, newY = y / simplificationIncrement;

                        int vertexIndex = (newY * newChunkSize) + newX;
                        int terrainMapX = (chunkX * chunkSize) - chunkX + x;
                        int terrainMapY = (chunkY * chunkSize) - chunkY + y;

                        // Calculate the vertex position
                        positions[vertexIndex] = CalculateWorldVertexPositionFromTerrainMapIndex(map, terrainMapX, terrainMapY, distanceBetweenNoiseSamples);

                        // Calculate the primary texture coordinates
                        UVs[vertexIndex] = new Vector2((float)x / chunkSize, (float)y / chunkSize);

                        // Set the colour
                        colours[vertexIndex] = TextureSettings.GetColour(map.Biomes[(terrainMapY * map.Width) + terrainMapX]);

                        if (newX >= 0 && newX < newChunkSize - 1 && newY >= 0 && newY < newChunkSize - 1)
                        {
                            UInt32 vertexIndexUnsigned = Convert.ToUInt32(vertexIndex);

                            // Set the triangles
                            triangles[triangleIndex] = vertexIndexUnsigned;
                            // Below
                            triangles[triangleIndex + 1] = vertexIndexUnsigned + chunkSizeU;
                            // Bottom right
                            triangles[triangleIndex + 2] = vertexIndexUnsigned + chunkSizeU + 1U;
                            triangleIndex += 3;

                            triangles[triangleIndex] = vertexIndexUnsigned;
                            // Bottom right
                            triangles[triangleIndex + 1] = vertexIndexUnsigned + chunkSizeU + 1U;
                            // Top right
                            triangles[triangleIndex + 2] = vertexIndexUnsigned + 1U;
                            triangleIndex += 3;
                        }
                    }
                }

                // Now set the sub mesh
                data.subMeshCount = 1;
                data.SetSubMesh(0, new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles));

                LODs.Add(meshes[baseMeshIndex + LODIndex]);
            }

            return LODs;
        }


        For(0, numChunksY, (int chunkY) =>
        {
            for (int chunkX = 0; chunkX < numChunksX; chunkX++)
            {
                Vector2Int chunk = new Vector2Int(chunkX, chunkY);

                // Populate the biomes array with data
                Biome.Type[] biomes = new Biome.Type[chunkSize * chunkSize];

                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        // Set the biomes
                        int terrainMapX = (chunkX * chunkSize) - chunkX + x;
                        int terrainMapY = (chunkY * chunkSize) - chunkY + y;
                        biomes[(y * chunkSize) + x] = map.Biomes[(terrainMapY * map.Width) + terrainMapX];
                    }
                }

                List<Mesh> LODs = GenerateLODsForChunk(chunkX, chunkY, biomes);

                chunkData.TryAdd(chunk, new TerrainChunkData(chunk, biomes, chunkSize, chunkSize, LODs, new List<WorldObjectData>()));
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

        // Calculate the mesh IDs of the highest LOD meshes
        int[] physicsBakeMeshIDs = new int[numChunksY * numChunksX];

        for (int y = 0; y < numChunksY; y++)
        {
            for (int x = 0; x < numChunksX; x++)
            {
                int baseMeshIndex = ((y * numChunksY) + x) * MeshSettings.LevelOfDetail.Count;
                physicsBakeMeshIDs[(y * numChunksX) + x] = meshes[baseMeshIndex].GetInstanceID();
            }
        }

        // Bake the mesh collision data in parallel 
        ForEach(physicsBakeMeshIDs, (id) =>
        {
            Physics.BakeMesh(id, false);
        });


        // Calculate which chunk each world object belongs to

        // SEQUENTIAL

        Dictionary<Vector2Int, Dictionary<GameObject, List<(Vector3, Vector3)>>> objectsForChunk = new Dictionary<Vector2Int, Dictionary<GameObject, List<(Vector3, Vector3)>>>();

        // TODO BETTER PERFORMANCE:
        foreach (TerrainMap.WorldObjectData obj in map.WorldObjects)
        {
            TerrainMapIndexToChunk(chunkSize - 1, new Vector2Int(obj.ClosestIndexX, obj.ClosestIndexY), out Vector2Int chunk, out Vector2Int _);

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
                UnityEngine.Debug.LogError("SplitIntoChunks: Missing dict chunk for world object");
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
                UnityEngine.Debug.LogError("ERROR");
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
            foreach (var attempt in TerrainSettings.ProceduralObjects)
            {
                if (attempt.Do && r.NextDouble() <= attempt.Chance)
                {
                    if (Utils.GetClosestIndex(pos, Vector2.zero, worldBoundsSize, map.Width, map.Height, out int x, out int y))
                    {
                        // Ensure we aren't on the edge of the terrain map
                        // Just do this for simplicity
                        if (x >= 1 && y >= 1 && x <= map.Width - 2 && y <= map.Height - 2)
                        {
                            // Closest terrain map index
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
                                    // Calculate the minimum height of the 2x2 of surrounding vertices
                                    float minimumHeight = Mathf.Min
                                    (
                                        map.Heights[terrainMapIndex],
                                        map.Heights[terrainMapIndex - map.Width - 1],
                                        map.Heights[terrainMapIndex - map.Width + 1],
                                        map.Heights[terrainMapIndex + map.Width - 1],
                                        map.Heights[terrainMapIndex + map.Width + 1]
                                    );

                                    // If we get here then this object must be valid at the position
                                    worldObjects.Add(new TerrainMap.WorldObjectData()
                                    {
                                        LocalPosition = new Vector3(pos.x, minimumHeight - WorldObjectYOffset, pos.y),
                                        Rotation = new Vector3(0, (float)r.NextDouble() * 360, 0),
                                        Prefab = attempt.Prefabs[r.Next(0, attempt.Prefabs.Count)],
                                        ClosestIndexX = x,
                                        ClosestIndexY = y,
                                    });

                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("GenerateTerrainMapProceduralObjects: Failed to choose closest terrain map index for position");
                    }
                }
            }
        }

        return worldObjects;
    }

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

    [Serializable]
    public class GenerationSettings
    {
        public System.Int32 Seed = 0;
        public bool GenerateLOD = false;
    }

}
