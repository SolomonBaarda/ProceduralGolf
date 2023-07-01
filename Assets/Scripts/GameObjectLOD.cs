using UnityEngine;

public class GameObjectLOD : MonoBehaviour
{
    public Mesh[] Meshes;

    [HideInInspector] public MeshFilter MeshFilter;
    [HideInInspector] public MeshCollider MeshCollider;

    private void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
        MeshCollider = GetComponent<MeshCollider>();
    }
}
