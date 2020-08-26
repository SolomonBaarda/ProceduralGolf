using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AcrossChunkBorderBiome
{
    public bool ShouldBeDestroyed = false;
    public bool NeedsUpdating = false;


    public HashSet<TerrainMap.Point> Vertices = new HashSet<TerrainMap.Point>();

    public Vector3 Centre => EvaluateMidpoint();



    public void Destroy()
    {
        Vertices.Clear();
    }



    private Vector3 EvaluateMidpoint()
    {
        if (Vertices.Count > 0)
        {
            TerrainMap.Point random = Vertices.FirstOrDefault();
            Vector3 min = random.LocalVertexPosition + random.Offset, max = min;

            foreach (TerrainMap.Point p in Vertices)
            {
                Vector3 v = p.LocalVertexPosition + p.Offset;

                if (v.x < min.x) { min.x = v.x; }
                if (v.z < min.z) { min.z = v.z; }

                if (v.x > max.x) { max.x = v.x; }
                if (v.z > max.z) { max.z = v.z; }
            }

            Vector3 centreOffset = (max + min) / 2;

            return centreOffset;
        }
        else
        {
            return default;
        }
    }






    public void Merge(ref AcrossChunkBorderBiome biome)
    {
        if (this != biome)
        {
            // Add all the vertices to this hole and remove it from the other
            Vertices.UnionWith(biome.Vertices);
            biome.Vertices.Clear();

            biome.ShouldBeDestroyed = true;

            // Assign the points hole to be this
            foreach (TerrainMap.Point p in Vertices)
            {
                //p.Hole = this;
            }


            NeedsUpdating = true;
        }
    }







}
