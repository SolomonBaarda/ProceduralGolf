using UnityEngine;

public static class TerrainMapGenerator
{


    public static TerrainMap Generate(int width, int height, Vector3[,] baseVertices, Bounds bounds,
            float[,] rawHeights, float[,] bunkersMask, float[,] holesMask, TerrainSettings terrainSettings)
    {
        terrainSettings.ValidateValues();

        // Create the map
        TerrainMap map = new TerrainMap(width, height, bounds)
        {
            Map = new TerrainMap.Point[width, height]
        };

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
                map.Map[x, y] = new TerrainMap.Point(baseVertices[x, y], bounds.center, pointHeight, b, atEdge);
            }
        }

        // Now set each neighbour
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {

                // Add the 3x3 of points as neighbours
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int pointX = x + i, pointY = y + j;

                        // Ensure within the array bounds
                        if (Utils.IsWithinArrayBounds(pointX, pointY, in map.Map))
                        {
                            // Don't add its self
                            if (pointX != x || pointY != y)
                            {
                                map.Map[x, y].Neighbours.Add(map.Map[pointX, pointY]);
                            }
                        }
                    }
                }
            }
        }


        // Return the terrain map 
        return map;
    }









    private static TerrainSettings.Biome CalculateBiomeForPoint(TerrainSettings settings, float rawBunker, float rawHole)
    {
        TerrainSettings.Biome b = settings.MainBiome;

        // Do a bunker
        if (settings.DoBunkers && !Mathf.Approximately(rawBunker, TerrainMap.Point.Empty))
        {
            b = TerrainSettings.Biome.Sand;
        }

        // Hole is more important
        if (!Mathf.Approximately(rawHole, TerrainMap.Point.Empty))
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




}




