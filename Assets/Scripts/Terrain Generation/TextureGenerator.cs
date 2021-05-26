using System;
using UnityEngine;

public static class TextureGenerator
{
    public static TextureData GenerateTextureDataForTerrainMap(in TerrainMap map, in TextureSettings settings)
    {
        int width = map.Width * 2 - 2, height = map.Height * 2 - 2;
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
                        // Add the colours in reverse order 
                        // This rotates the texture 180 degrees so it matches with the terrain
                        colours[colours.Length - (y * width + x) - 1] = c;
                    }
                }
            }
        }

        return new TextureData(width, height, colours, settings);
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
