using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Settings/Texture")]
public class TextureSettings : VariablePreset
{
    public List<BiomeColour> Colours = new List<BiomeColour>();
    private Dictionary<Biome.Type, Color32> colours = new Dictionary<Biome.Type, Color32>();



    public static void ApplyToMaterial(Material m, in Texture2D colourMap, Vector2 maintextureTiling)
    {
        // Add the colour map
        m.SetTexture("_ColourMap", colourMap);

        // Set the texture tiling
        m.SetVector("_MainTexTiling", maintextureTiling);
        m.SetVector("_BunkerTexTiling", maintextureTiling);

        // Set all the colours used in the colour map 
        //m.SetColor("_ColourMain", main);
        //m.SetColor("_ColourHole", hole);
        //m.SetColor("_ColourBunker", bunker);
    }

    public override void ValidateValues()
    {
    }

    public void AddColoursToDictionary()
    {
        colours.Clear();
        foreach (BiomeColour c in Colours)
        {
            colours.Add(c.Biome, c.Colour);
        }
    }

    public Color32 GetColour(Biome.Type biome)
    {
        // Find the colour
        if (colours.TryGetValue(biome, out Color32 c))
        {
            return c;
        }

        return Color.white;
    }


    [System.Serializable]
    public class BiomeColour
    {
        public Biome.Type Biome;
        public Color32 Colour;
    }
}
