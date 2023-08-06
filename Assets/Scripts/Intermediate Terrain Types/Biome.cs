using System.Collections.Generic;

public static class Biome
{
    public enum Type
    {
        None,
        LongGrass,
        MediumGrass,
        ShortGrass,
        Sand,
        Water,
        Ice,
        Snow,
        Regolith,
    }

    public static HashSet<Biome.Type> GetAllBiomes()
    {
        return new HashSet<Biome.Type>()
        {
            Type.None,
            Type.LongGrass,
            Type.MediumGrass,
            Type.ShortGrass,
            Type.Sand,
            Type.Water,
            Type.Ice,
            Type.Snow,
            Type.Regolith,
        };
    }
}