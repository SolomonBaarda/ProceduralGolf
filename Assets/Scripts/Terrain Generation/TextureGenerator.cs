using UnityEngine;

public static class TextureGenerator
{












    public static Color[] GenerateColourMap(in TerrainMapGenerator.TerrainMap map, TextureSettings settings)
    {
        settings.ValidateValues();

        Color[] colours = new Color[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                Color c = settings.GetColour(map.Map[x, y].Biome);

                colours[y * map.Width + x] = c;
            }
        }

        return colours;
    }



    public static Texture2D GenerateTexture(in TerrainMapGenerator.TerrainMap terrainMap, in TextureSettings settings)
    {
        return GenerateTexture(GenerateColourMap(terrainMap, settings), terrainMap.Width, terrainMap.Height);
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
