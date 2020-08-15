using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Settings/Texture")]
public class TextureSettings : VariablePreset
{
    public List<BiomeColour> Colours = new List<BiomeColour>();



    public static void ApplyToMaterial(ref Material m, Texture2D colourMap)
    {
        m.SetTexture("_ColourMap", colourMap);
    }

    public override void ValidateValues()
    {
    }


    public Color GetColour(TerrainSettings.Biome biome)
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
        public TerrainSettings.Biome Biome;
        public Color Colour;
    }
}
