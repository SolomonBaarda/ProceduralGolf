using UnityEditor.Experimental.TerrainAPI;
using UnityEngine;

public static class TextureGenerator
{



    public static Color[] GenerateColourMap(in TerrainMap map, TextureSettings settings)
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



    public static TextureData GenerateTextureData(in TerrainMap terrainMap, in TextureSettings settings)
    {
        return new TextureData(terrainMap.Width, terrainMap.Height, GenerateColourMap(terrainMap, settings));
    }



    public static Texture2D GenerateTexture(in TextureData data)
    {
        Texture2D t = new Texture2D(data.Width, data.Height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            
        };

        t.SetPixels(data.ColourMap);
        t.Apply();

        return t;
    }



    public struct TextureData
    {
        public int Width, Height;
        public Color[] ColourMap;

        public TextureData(int width, int height, Color[] colours)
        {
            Width = width;
            Height = height;
            ColourMap = colours;
        }
    }

}
