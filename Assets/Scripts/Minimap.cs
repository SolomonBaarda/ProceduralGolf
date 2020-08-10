using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Minimap
{






    public static Texture2D GenerateFullMap(in TerrainMap[,] nearbyMaps)
    {
        if (nearbyMaps != null && nearbyMaps.Length > 0)
        {
            int mapsX = nearbyMaps.GetLength(0), mapsY = nearbyMaps.GetLength(1);
            int width = nearbyMaps[0, 0].Width, height = nearbyMaps[0, 0].Height;
            int totalWidth = width * mapsX, totalHeight = height * mapsY;

            Color[] colours = new Color[totalWidth * totalHeight];

            // Loop over each map
            for (int y = 0; y < mapsY; y++)
            {
                for (int x = 0; x < mapsX; x++)
                {
                    TerrainMap m = nearbyMaps[x, y];

                    // Generate the texture
                    Texture2D t = TextureGenerator.GenerateTexture(in m);

                    Color[] mapPixels = t.GetPixels();
                    for(int i = 0; i < mapPixels.Length; i++)
                    {
                        //int index = i / width 
                    }

                }
            }





            // Create the texture
            Texture2D combined = new Texture2D(totalWidth, totalHeight);
            combined.SetPixels(colours);

            combined.Apply();

            return combined;
        }

        return null;
    }




}
