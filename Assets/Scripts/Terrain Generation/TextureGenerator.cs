using UnityEngine;

public static class TextureGenerator
{












    public static Color[] GenerateColourMap(in TerrainMap map)
    {
        Color[] colours = new Color[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                Color c = Color.black;
                switch (map.Map[x, y].Biome)
                {
                    case TerrainSettings.Biome.Grass:
                        c = Color.green;
                        break;
                    case TerrainSettings.Biome.Sand:
                        c = Color.yellow;
                        break;
                    case TerrainSettings.Biome.Hole:
                        c = Color.red;
                        break;
                    case TerrainSettings.Biome.Water:
                        c = Color.blue;
                        break;
                    case TerrainSettings.Biome.Ice:
                        c = Color.white;
                        break;
                }

                colours[y * map.Width + x] = c;
            }
        }

        return colours;
    }



    public static Texture2D GenerateTexture(in TerrainMap terrainMap)
    {
        return GenerateTexture(GenerateColourMap(terrainMap), terrainMap.Width, terrainMap.Height);
    }



    public static Texture2D GenerateTexture(in Color[] colourMap, int width, int height)
    {
        Texture2D t = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };


        //Debug.Log("Creating texture with width " + width + " and height " + height);

        t.SetPixels(colourMap);
        t.Apply();

        return t;
    }

}
