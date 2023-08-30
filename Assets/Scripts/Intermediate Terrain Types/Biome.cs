using System;
using System.Collections.Generic;
using System.Linq;

public static class Biome
{
    public enum Type
    {
        None = 0,
        LongGrass = 1,
        MediumGrass = 2,
        ShortGrass = 3,
        Mud = 4,
        NormalSand = 5,
        HardSand = 6,
        SoftSand = 7,
        Ice = 8,
        Snow = 9,
        Stone = 10,
    }

    public static HashSet<Type> GetAllBiomes()
    {
        return Enum.GetValues(typeof(Type))
            .OfType<Type>()
            .ToHashSet();
    }
}