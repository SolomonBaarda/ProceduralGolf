using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;

    public TerrainChunkManager TerrainChunkManager;

    public bool IsGenerating { get; private set; } = false;

    [Header("Settings")]
    public MeshSettings MeshSettingsVisual;
    public MeshSettings MeshSettingsCollider;
    public bool UseSameMesh = true;
    [Space]
    public NoiseSettings NoiseSettings_Green;
    public NoiseSettings NoiseSettings_Bunker;
    public NoiseSettings NoiseSettings_Holes;
    public TerrainSettings TerrainSettings_Green;

    [Space]
    public int Seed = 0;
    public bool DoRandomSeed = false;

    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;


    public void GenerateInitialTerrain()
    {
        if (!IsGenerating)
        {
            if (DoRandomSeed)
            {
                Seed = Noise.RandomSeed;
            }

            GenerateInitialArea(Seed, 1);
        }
    }


    public void Clear()
    {
        if (!IsGenerating)
        {
            TerrainChunkManager.Clear();
        }
    }



    private void GenerateInitialArea(int seed, int chunksFromOrigin)
    {
        DateTime before = DateTime.Now;

        IsGenerating = true;
        chunksFromOrigin = Mathf.Abs(chunksFromOrigin);

        // Reset the whole terrain map
        TerrainChunkManager.Clear();

        // Generate in that area
        for (int y = -chunksFromOrigin; y <= chunksFromOrigin; y++)
        {
            for (int x = -chunksFromOrigin; x <= chunksFromOrigin; x++)
            {
                TryGenerateChunk(new Vector2Int(x, y), seed);
            }
        }

        Debug.Log("Generated in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds.");
        IsGenerating = false;
    }



    public void TryGenerateChunk(Vector2Int chunk, int seed)
    {
        if (!TerrainChunkManager.TerrainChunkExists(chunk))
        {
            // Get the chunk bounds
            Bounds chunkBounds = TerrainChunkManager.CalculateTerrainChunkBounds(chunk);

            // Get the vertex points
            Vector3[,] vertices = CalculateVertexPointsForChunk(chunkBounds, TerrainSettings_Green);
            Vector3[,] localVertexPositions = CalculateLocalVertexPointsForChunk(vertices, chunkBounds.center);
            Vector2[,] noiseSamplePoints = ConvertWorldPointsToPerlinSample(vertices);

            int width = vertices.GetLength(0), height = vertices.GetLength(1);

            // Heights
            float[,] heightsRaw = Noise.Perlin(NoiseSettings_Green, seed, noiseSamplePoints);

            // Bunkers
            int bunkerSeed = Noise.Seed(seed.ToString());
            float[,] bunkerRaw = Noise.Perlin(NoiseSettings_Bunker, bunkerSeed, noiseSamplePoints);

            // Holes
            int holeSeed = Noise.Seed(bunkerSeed.ToString());
            float[,] holesRaw = Noise.Perlin(NoiseSettings_Holes, holeSeed, noiseSamplePoints);

            //Debug.Log("bunker before " + chunk.ToString() + TerrainMapGenerator.DebugMinMax(bunkerRaw));

            // Create masks from the bunkers and holes
            float[,] bunkerShapeMask = new float[width, height], holeShapeMask = new float[width,height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Bunker
                    if (bunkerRaw[x, y] >= TerrainSettings_Green.BunkerNoiseThresholdMinMax.x && bunkerRaw[x, y] <= TerrainSettings_Green.BunkerNoiseThresholdMinMax.y)
                    {
                        bunkerShapeMask[x, y] = bunkerRaw[x, y];
                    }
                    else
                    {
                        bunkerShapeMask[x, y] = TerrainMap.Point.Empty;
                    }

                    // Hole
                    if (holesRaw[x, y] >= TerrainSettings_Green.HoleNoiseThresholdMinMax.x && holesRaw[x, y] <= TerrainSettings_Green.HoleNoiseThresholdMinMax.y)
                    {
                        holeShapeMask[x, y] = holesRaw[x, y];
                    }
                    else
                    {
                        holeShapeMask[x, y] = TerrainMap.Point.Empty;
                    }
                }
            }

            //Debug.Log("bunker after " + chunk.ToString() + TerrainMapGenerator.DebugMinMax(bunkerShapeMask));

            // Get the terrain map
            TerrainMap terrainMap = new TerrainMap(width, height, localVertexPositions, heightsRaw, bunkerShapeMask, holeShapeMask, TerrainSettings_Green);

            
            /*
            List<Hole> holesInThisChunk = Hole.CalculateHoles(ref terrainMap);
            Debug.Log(holesInThisChunk.Count + " holes in chunk " + chunk.ToString());

            foreach(Hole h in holesInThisChunk)
            {
                float holeHeight = h.EvaluateHeight();

                // Overwrite all the heights for this hole
                foreach(TerrainMap.Point p in h.HoleVertices)
                {
                    p.Height = holeHeight;
                }

                // World pos of the centre of the hole
                Vector3 holeCentre = chunkBounds.center + h.EvaluateMidpointLocal();

                Debug.DrawLine(holeCentre, holeCentre + UP * 100, Color.red, 100);
            }
            */
            

            TerrainChunkManager.AddNewChunk(chunk, terrainMap, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer, MeshSettingsVisual, MeshSettingsCollider, UseSameMesh);
        }
    }





    public void CheckNearbyChunks(Vector3 position, float viewDistanceWorldUnits)
    {
        // Calculate the centre chunk
        Vector2Int centre = TerrainChunkManager.WorldToChunk(position);
        int chunks = Mathf.RoundToInt(viewDistanceWorldUnits / TerrainChunkManager.ChunkSizeWorldUnits);

        List<Vector2Int> nearbyChunks = new List<Vector2Int>();

        // Generate in that area
        for (int y = -chunks; y <= chunks; y++)
        {
            for (int x = -chunks; x <= chunks; x++)
            {
                Vector2Int chunk = new Vector2Int(centre.x + x, centre.y + y);
                TryGenerateChunk(chunk, Seed);
                nearbyChunks.Add(chunk);
            }
        }

        // Set only those chunks to be visible
        TerrainChunkManager.SetVisibleChunks(nearbyChunks);
    }




    private Vector2[,] ConvertWorldPointsToPerlinSample(Vector3[,] points)
    {
        int width = points.GetLength(0), height = points.GetLength(1);
        Vector2[,] points2D = new Vector2[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                points2D[x, y] = new Vector2(points[x, y].x, points[x, y].z);
            }
        }

        return points2D;
    }




    public static Vector3 CalculateDistanceBetweenVertices(Bounds b, int divisions)
    {
        return (b.max - b.min) / divisions;
    }





    public static Vector3[,] CalculateVertexPointsForChunk(in Bounds chunk, TerrainSettings settings)
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

    public static Vector3[,] CalculateLocalVertexPointsForChunk(in Vector3[,] worldPoints, in Vector3 centre)
    {
        int width = worldPoints.GetLength(0), height = worldPoints.GetLength(1);
        Vector3[,] localPositions = new Vector3[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                localPositions[x, y] = worldPoints[x, y] - centre;
            }
        }

        return localPositions;
    }




}
