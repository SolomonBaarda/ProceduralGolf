using System;
using UnityEngine;

public static class TextureGenerator
{
    public static TextureData GenerateTextureDataForTerrainMap(in TerrainMap terrainMap, in TextureSettings settings)
    {
        Color32[] colours = GenerateColourMap(terrainMap, settings, out int width, out int height);
        TextureData d = new TextureData(width, height, colours, settings);

        // Reverse the colour map - rotates 180 degrees 
        // For some reason the texture needs this
        Array.Reverse(d.ColourMap);

        return d;
    }

    private static Color32[] GenerateColourMap(in TerrainMap map, in TextureSettings settings, out int width, out int height)
    {
        width = map.Width * 2 - 2;
        height = map.Height * 2 - 2;

        Color32[] colours = new Color32[width * height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                Color32 c = settings.GetColour(map.Biomes[y * map.Width + x]);

                TrySetColour(2 * x, 2 * y, width, height);
                TrySetColour(2 * x - 1, 2 * y, width, height);
                TrySetColour(2 * x, 2 * y - 1, width, height);
                TrySetColour(2 * x - 1, 2 * y - 1, width, height);

                void TrySetColour(int x, int y, int width, int height)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        colours[y * width + x] = c;
                    }
                }
            }
        }

        return colours;
    }

    public static Texture2D GenerateBiomeColourMap(in TextureData data)
    {
        // Create the texture from the data
        Texture2D t = new Texture2D(data.Width, data.Height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        t.SetPixels32(data.ColourMap);
        t.Apply();
        t.Compress(false);

        return t;
    }



    public struct TextureData
    {
        public int Width, Height;
        public Color32[] ColourMap;
        public TextureSettings Settings;

        public TextureData(int width, int height, in Color32[] colours, in TextureSettings settings)
        {
            Width = width;
            Height = height;
            ColourMap = colours;
            Settings = settings;
        }
    }

}
