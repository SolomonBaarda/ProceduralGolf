using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProceduralHoleMesh
{


    public static void CreateHole(Vector3 centre, float radius, int detail = 3)
    {
        radius = Mathf.Max(0, radius);
        detail = Mathf.Max(2, detail);

        List<Mesh> meshes = GetMeshes(centre, radius, GroundCheck.GroundMask);


    }









    public static List<Mesh> GetMeshes(Vector3 holeCentre, float holeRadius, int layerMask)
    {
        Collider[] collisions = Physics.OverlapSphere(holeCentre, holeRadius, layerMask);

        List<Mesh> meshes = new List<Mesh>();
        foreach (Collider c in collisions)
        {
            MeshFilter f = c.gameObject.GetComponent<MeshFilter>();
            if (f != null && f.sharedMesh != null)
            {
                meshes.Add(f.sharedMesh);
            }
        }

        return meshes;
    }

}
