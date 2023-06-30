using UnityEngine;

public static class Biome
{

    public static Type GetBiomeSamplePoint(Collider collider, Vector3 worldPos)
    {
        if (collider != null)
        {
            TerrainChunk c = collider.gameObject.GetComponent<TerrainChunk>();
            MeshCollider m = collider.gameObject.GetComponent<MeshCollider>();

            if (c != null)
            {
                if (Utils.GetClosestIndex(worldPos, m.bounds.min, m.bounds.max, c.Data.Width, c.Data.Height, out int x, out int y))
                {
                    return c.Data.Biomes[(y * c.Data.Width) + x];
                }
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


