using System;
using System.Collections.Generic;

public static class Biome
{
    public enum Type
    {
        None = 0,
        LongGrass = 1,
        MediumGrass = 2,
        ShortGrass = 3,
        Water = 4,
        NormalSand = 5,
        HardSand = 6,
        SoftSand = 7,
        Ice = 8,
        Snow = 9,
        Regolith = 10,
    }

    public static HashSet<Biome.Type> GetAllBiomes()
    {
        // TODO USE Enum.GetValues()

        return new HashSet<Biome.Type>()
        {
            Type.None,
            Type.LongGrass,
            Type.MediumGrass,
            Type.ShortGrass,
            Type.Water,
            Type.NormalSand,
            Type.HardSand,
            Type.SoftSand,
            Type.Ice,
            Type.Snow,
            Type.Regolith,
        };
    }
}