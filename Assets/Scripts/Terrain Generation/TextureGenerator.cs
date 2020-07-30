using UnityEngine;

public static class TextureGenerator
{

    public static Color[] GenerateColourMap(TerrainMapGenerator.TerrainMap map)
    {
        Color[] colours = new Color[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                Color c = Color.white;
                if (map.Map[x, y].Biome == TerrainSettings.Biome.Grass)
                {
                    c = Color.green;
                }
                else if (map.Map[x, y].Biome == TerrainSettings.Biome.Sand)
                {
                    c = Color.black;
                }

                colours[y * map.Width + x] = c;
            }
        }

        return colours;
    }



    public static Texture2D GenerateTexture(TerrainMapGenerator.TerrainMap terrainMap)
    {
        return GenerateTexture(GenerateColourMap(terrainMap), terrainMap.Width, terrainMap.Height);
    }



    public static Texture2D GenerateTexture(Color[] colourMap, int width, int height)
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
