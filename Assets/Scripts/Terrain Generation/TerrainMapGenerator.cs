using UnityEngine;

public static class TerrainMapGenerator
{

    public class TerrainMap
    {
        public int Width, Height;

        // Map
        public Point[,] Map;

        // Settings
        public TerrainSettings TerrainSettings;

        public TerrainMap(int width, int height, Vector3[,] vertices, float[,] heights, float[,] bunkers, TerrainSettings terrainSettings)
        {
            Width = width;
            Height = height;

            TerrainSettings = terrainSettings;

            // Create the map
            Map = new Point[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Assign the terrain point
                    Map[x, y] = new Point(terrainSettings, vertices[x, y], heights[x, y], bunkers[x, y]);
                }
            }
        }








        public class Point
        {
            public Vector3 LocalVertexBasePosition;

            private float BaseHeight;
            private float Bunker;
            public const float NoBunker = 0f;

            public TerrainSettings.Biome Biome;
            public float Height;


            public Point(TerrainSettings settings, Vector3 localVertexPos, float baseHeight, float bunker)
            {
                LocalVertexBasePosition = localVertexPos;
                BaseHeight = baseHeight;
                Bunker = bunker;

                Biome = CalculateBiome(settings);
                Height = CalculateFinalHeight(settings);
            }



            private TerrainSettings.Biome CalculateBiome(TerrainSettings settings)
            {
                TerrainSettings.Biome b = settings.Main;

                // Do a bunker
                if (settings.DoBunkers && !Mathf.Approximately(Bunker, NoBunker))
                {
                    b = TerrainSettings.Biome.Sand;
                }

                return b;
            }


            private float CalculateFinalHeight(TerrainSettings settings)
            {
                // Calculate the height to use
                float height = BaseHeight;
                if (settings.UseCurve)
                {
                    height = settings.HeightDistribution.Evaluate(BaseHeight);
                }

                // And apply the scale
                height *= settings.HeightMultiplier;


                // Add the bunker now
                if (settings.DoBunkers)
                {
                    height -= Bunker * settings.BunkerMultiplier;
                }

                return height;
            }
        }
    }


    public static string DebugMinMax(float[,] array)
    {
        if (array != null)
        {
            int width = array.GetLength(0), height = array.GetLength(1);
            float min = array[0, 0], max = array[0, 0];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float curr = array[x, y];
                    if (curr < min)
                    {
                        min = curr;
                    }
                    if (curr > max)
                    {
                        max = curr;
                    }
                }
            }

            return "min: " + min + " max: " + max;
        }

        return "";
    }
}
