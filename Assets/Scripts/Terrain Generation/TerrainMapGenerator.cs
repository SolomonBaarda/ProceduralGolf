using UnityEngine;

public static class TerrainMapGenerator
{


    public static TerrainMapData Generate(int width, int height, Vector3[,] baseVertices, Vector3 offset,
            float[,] rawHeights, float[,] bunkersMask, float[,] holesMask, TerrainSettings terrainSettings)
    {
        // Create the map
        PointData[,] map = new PointData[width, height];

        // Assign all the terrain point vertices
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculate the point values
                bool atEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;

                float pointHeight = CalculateFinalHeight(terrainSettings, rawHeights[x, y], bunkersMask[x, y]);
                TerrainSettings.Biome b = CalculateBiomeForPoint(terrainSettings, bunkersMask[x, y], holesMask[x, y]);

                // Assign the terrain point
                map[x, y] = new PointData(baseVertices[x, y].x, baseVertices[x, y].y, baseVertices[x, y].z, offset.x, offset.y, offset.z, pointHeight, b, atEdge);
            }
        }


        // Return the new terrain map struct
        return new TerrainMapData(width, height, offset.x, offset.y, offset.z, map);
    }






    public struct TerrainMapData
    {
        public int Width, Height;

        /// <summary>
        /// The maximum LOD Terrain data.
        /// </summary>
        public PointData[,] Map;

        public float OffsetX, OffsetY, OffsetZ;


        public TerrainMapData(int width, int height, float offsetX, float offsetY, float offsetZ, PointData[,] map)
        {
            Width = width;
            Height = height;

            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;

            Map = map;
        }
    }






    private static TerrainSettings.Biome CalculateBiomeForPoint(TerrainSettings settings, float rawBunker, float rawHole)
    {
        TerrainSettings.Biome b = settings.MainBiome;

        // Do a bunker
        if (settings.DoBunkers && !Mathf.Approximately(rawBunker, PointData.Empty))
        {
            b = TerrainSettings.Biome.Sand;
        }

        // Hole is more important
        if (!Mathf.Approximately(rawHole, PointData.Empty))
        {
            b = TerrainSettings.Biome.Hole;
        }

        return b;
    }


    private static float CalculateFinalHeight(TerrainSettings settings, float rawHeight, float rawBunker)
    {
        // Calculate the height to use
        float height = rawHeight;
        if (settings.UseCurve)
        {
            height = settings.HeightDistribution.Evaluate(rawHeight);
        }

        // And apply the scale
        height *= settings.HeightMultiplier;


        // Add the bunker now
        if (settings.DoBunkers)
        {
            height -= rawBunker * settings.BunkerMultiplier;
        }

        /*
        if (Biome == TerrainSettings.Biome.Hole)
        {
            height = 0.75f * settings.HeightMultiplier;
        }
        */


        return height;
    }




    public struct PointData
    {
        public const float Empty = 0f;

        public float LocalBaseX, LocalBaseY, LocalBaseZ;
        public float OffsetX, OffsetY, OffsetZ;


        public bool IsAtEdgeOfMesh;

        public TerrainSettings.Biome Biome;
        public float OriginalHeight;

        public PointData(float localX, float localY, float localZ, float offsetX, float offsetY, float offsetZ, float originalHeight, TerrainSettings.Biome biome, bool isAtEdgeOfMesh)
        {
            LocalBaseX = localX;
            LocalBaseY = localY;
            LocalBaseZ = localZ;
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;

            IsAtEdgeOfMesh = isAtEdgeOfMesh;

            Biome = biome;
            OriginalHeight = originalHeight;
        }
    }


}




