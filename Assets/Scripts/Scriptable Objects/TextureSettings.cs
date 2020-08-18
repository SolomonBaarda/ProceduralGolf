using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Settings/Texture")]
public class TextureSettings : VariablePreset
{
    public List<BiomeColour> Colours = new List<BiomeColour>();



    public static void ApplyToMaterial(ref Material m, Texture2D colourMap, Vector2 maintextureTiling, Color main, Color hole, Color bunker)
    {
        // Add the colour map
        m.SetTexture("_ColourMap", colourMap);

        // Set the texture tiling
        m.SetVector("_MainTexTiling", maintextureTiling);
        m.SetVector("_BunkerTexTiling", maintextureTiling);

        // Set all the colours used in the colour map 
        m.SetColor("_ColourMain", main);
        m.SetColor("_ColourHole", hole);
        m.SetColor("_ColourBunker", bunker);
    }

    public override void ValidateValues()
    {
    }


    public Color GetColour(Biome.Type biome)
    {
        // Find the colour
        Color c = Colours.Find((x) => x.Biome == biome).Colour;

        if (c == null)
        {
            c = Color.white;
        }

        return c;
    }


    [System.Serializable]
    public struct BiomeColour
    {
        public Biome.Type Biome;
        public Color Colour;
    }
}
