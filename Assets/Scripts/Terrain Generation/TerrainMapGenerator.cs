using UnityEngine;

public static class TerrainMapGenerator
{

    public class TerrainMap
    {
        public int Width, Height;

        /// <summary>
        /// The maximum LOD Terrain data.
        /// </summary>
        public Point[,] Map;

        // Settings
        public TerrainSettings TerrainSettings;

        public TerrainMap(int width, int height, Vector3[,] baseVertices, 
            float[,] rawHeights, float[,] bunkersMask, float[,] holesMask, TerrainSettings terrainSettings)
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
                    Map[x, y] = new Point(terrainSettings, baseVertices[x, y], rawHeights[x, y], bunkersMask[x, y], holesMask[x,y]);
                }
            }
        }






        public class Point
        {
            public const float Empty = 0f;


            public Vector3 LocalVertexBasePosition;
            // Calculate the point of the vertex
            public Vector3 LocalVertexPosition => LocalVertexBasePosition + (TerrainGenerator.UP * Height);

            private readonly float rawHeight;
            private readonly float rawBunker;
            private readonly float rawHole;

            public TerrainSettings.Biome Biome;
            public float Height;


            public Point(TerrainSettings settings, Vector3 localVertexPos, float rawHeight, float rawBunker, float rawHole)
            {
                LocalVertexBasePosition = localVertexPos;
                this.rawHeight = rawHeight;
                this.rawBunker = rawBunker;
                this.rawHole = rawHole;

                Biome = CalculateBiome(settings);
                Height = CalculateFinalHeight(settings);
            }



            private TerrainSettings.Biome CalculateBiome(TerrainSettings settings)
            {
                TerrainSettings.Biome b = settings.MainBiome;

                // Do a bunker
                if (settings.DoBunkers && !Mathf.Approximately(rawBunker, Empty))
                {
                    b = TerrainSettings.Biome.Sand;
                }

                // Hole is more important
                if(!Mathf.Approximately(rawHole, Empty))
                {
                    b = TerrainSettings.Biome.Hole;
                }

                return b;
            }


            private float CalculateFinalHeight(TerrainSettings settings)
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


                if(Biome == TerrainSettings.Biome.Hole)
                {
                    height = 0.75f * settings.HeightMultiplier;
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
