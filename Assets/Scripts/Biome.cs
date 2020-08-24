using UnityEngine;

public static class Biome
{
    private static readonly Color Grass = Color.green, Sand = Color.yellow, Hole = Color.grey, Water = Color.blue, Ice = Color.white, None = Color.black;




    public static Type GetBiomeSamplePoint(Collider collider, Vector3 worldPos)
    {
        if (collider != null)
        {
            TerrainChunk c = collider.gameObject.GetComponent<TerrainChunk>();

            if (c != null)
            {
                return Utils.GetClosestTo(worldPos, c.Bounds.min, c.Bounds.max, c.Biomes);
            }
        }

        return default;
    }







    public static Color BiomeToColour(Type t)
    {
        switch (t)
        {
            case Type.Grass:
                return Grass;
            case Type.Sand:
                return Sand;
            case Type.Hole:
                return Hole;
            case Type.Water:
                return Water;
            case Type.Ice:
                return Ice;
        }

        return None;
    }


    public static Type ColourToBiome(Color c)
    {
        if (c.Equals(Grass))
        {
            return Type.Grass;
        }
        else if (c.Equals(Sand))
        {
            return Type.Sand;
        }
        else if (c.Equals(Hole))
        {
            return Type.Hole;
        }
        else if (c.Equals(Water))
        {
            return Type.Water;
        }
        else if (c.Equals(Ice))
        {
            return Type.Ice;
        }

        return Type.None;
    }



    public enum Type
    {
        None,
        Grass,
        Sand,
        Hole,
        Water,
        Ice,
    }
}


