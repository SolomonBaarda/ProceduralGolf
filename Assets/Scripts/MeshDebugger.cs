using UnityEngine;

public class MeshDebugger : MonoBehaviour
{
    public MeshFilter MeshFilter;
    public Mesh Mesh => MeshFilter.mesh;
    public MeshRenderer MeshRenderer;

    private void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
        MeshRenderer = GetComponent<MeshRenderer>();
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
