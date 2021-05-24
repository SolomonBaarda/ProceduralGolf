using UnityEngine;

public static class Biome
{

    public static Type GetBiomeSamplePoint(Collider collider, Vector3 worldPos)
    {
        if (collider != null)
        {
            TerrainChunk c = collider.gameObject.GetComponent<TerrainChunk>();

            if (c != null)
            {
                return Utils.GetClosestTo(worldPos, c.Bounds.min, c.Bounds.max, c.Biomes, out int _, out int _);
            }
        }

        return Type.None;
    }


    

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




    public enum Decoration
    {
        None,
        Tree,
        Rock,
    }
}


