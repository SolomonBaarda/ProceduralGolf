using UnityEngine;

public static class TextureGenerator
{
    public static TextureData CombineChunkTextureData(TextureData[,] textureData, int dataWidth, int dataHeight, in TextureSettings settings)
    {
        int width = textureData.GetLength(0) * dataWidth, height = textureData.GetLength(1) * dataHeight;
        Color32[] colours = new Color32[width * height];

        for (int chunkY = 0; chunkY < textureData.GetLength(1); chunkY++)
        {
            for (int chunkX = 0; chunkX < textureData.GetLength(0); chunkX++)
            {
                TextureData d = textureData[chunkX, chunkY];

                for (int pixelY = 0; pixelY < d.Height; pixelY++)
                {
                    for (int pixelX = 0; pixelX < d.Width; pixelX++)
                    {
                        int dataX = (chunkX * d.Width) + pixelX, dataY = (chunkY * d.Height) + pixelY;
                        int index = (dataY * width) + dataX;

                        // Add the colours in reverse order - the original colour map is reversed
                        colours[index] = d.ColourMap[d.ColourMap.Length - ((pixelY * d.Width) + pixelX) - 1];

                    }
                }
            }
        }

        return new TextureData(width, height, colours, settings);
    }

    public static TextureData GenerateTextureDataForChunk(in Biome.Type[] biomes, int width, int height, in TextureSettings settings)
    {
        int textureWidth = (width * 2) - 2, textureHeight = (height * 2) - 2;

        Color32[] colours = new Color32[textureWidth * textureHeight];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 c = settings.GetColour(biomes[(y * width) + x]);

                TrySetColour(2 * x, 2 * y, textureWidth, textureHeight);
                TrySetColour((2 * x) - 1, 2 * y, textureWidth, textureHeight);
                TrySetColour(2 * x, (2 * y) - 1, textureWidth, textureHeight);
                TrySetColour((2 * x) - 1, (2 * y) - 1, textureWidth, textureHeight);

                void TrySetColour(int x, int y, int width, int height)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        // Add the colours in reverse order 
                        // This rotates the texture 180 degrees so it matches with the terrain
                        colours[colours.Length - ((y * width) + x) - 1] = c;
                    }
                }
            }
        }

        return new TextureData(textureWidth, textureHeight, colours, settings);
    }

    public static Texture2D GenerateTextureFromData(in TextureData data)
    {
        // Create the texture from the data
        Texture2D t = new Texture2D(data.Width, data.Height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        t.SetPixels32(data.ColourMap);
        t.Apply();
        //t.Compress(false);

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
