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





    public static Color32[] GenerateColourMap(in TerrainMap map, in TextureSettings settings)
    {
        settings.ValidateValues();

        Color32[] colours = new Color32[map.Width * map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                int index = y * map.Width + x;
                Color32 c = settings.GetColour(map.Biomes[index]);
                colours[index] = c;
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
