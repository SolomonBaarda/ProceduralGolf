using System;
using UnityEngine;

public static class TextureGenerator
{


    public static Texture2D GenerateBiomeColourMap(in TerrainMap terrainMap, in TextureSettings settings)
    {
        return GenerateBiomeColourMap(GenerateTextureData(terrainMap, settings));
    }


    public static Texture2D GenerateBiomeColourMap(TextureData data)
    {
        // Reverse the colour map - rotates 180 degrees 
        // For some reason the texture needs this
        Array.Reverse(data.ColourMap);

        // Create the texture from the data
        return GenerateTexture(data);
    }





    public static Color[] GenerateColourMap(in TerrainMap map, in TextureSettings settings)
    {
        settings.ValidateValues();

        Color[] colours = new Color[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                Color c = settings.GetColour(map.Points[x, y].Biome);

                colours[y * map.Width + x] = c;
            }
        }

        return colours;
    }



    public static TextureData GenerateTextureData(in TerrainMap terrainMap, in TextureSettings settings)
    {
        return new TextureData(terrainMap.Width, terrainMap.Height, GenerateColourMap(terrainMap, settings), settings);
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

        t.Compress(false);

        return t;
    }



    public struct TextureData
    {
        public int Width, Height;
        public Color[] ColourMap;
        public TextureSettings Settings;

        public TextureData(int width, int height, in Color[] colours, in TextureSettings settings)
        {
            Width = width;
            Height = height;
            ColourMap = colours;
            Settings = settings;
        }
    }

}
