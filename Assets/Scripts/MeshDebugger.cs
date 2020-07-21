using UnityEngine;

public class MeshDebugger : MonoBehaviour
{
    public MeshFilter MeshFilter;
    public Mesh Mesh;

    private void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
        Mesh = MeshFilter.mesh;
    }


    private void OnDrawGizmosSelected()
    {
        if (Mesh != null)
        {
            Vector3 offset = transform.position;

            // Draw all normals for the mesh
            for (int i = 0; i < Mesh.vertexCount; i++)
            {
                Vector3 vertex = Mesh.vertices[i] + offset;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(vertex, vertex + Mesh.normals[i]);
            }
        }
    }



}
