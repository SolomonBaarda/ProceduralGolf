using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Settings/Texture")]
public class TextureSettings : VariablePreset
{
    public List<BiomeColour> Colours = new List<BiomeColour>();



    public void ApplyToMaterial(ref Material m, Texture2D colourMap, Vector2 tiling)
    {
        m.SetTexture("_ColourMap", colourMap);

        m.SetVector("_MainTexTiling", tiling);
        m.SetVector("_BunkerTexTiling", tiling);

        m.SetColor("_Sand", GetColour(Biome.Type.Sand));
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
