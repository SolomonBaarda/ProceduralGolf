using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    public Vector2Int Position { get; private set; }

    public bool IsVisible => gameObject.activeSelf;


    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;


    public TerrainChunkData Data;


    private void Awake()
    {
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
    }

    public void Initialise(Vector2Int position, TerrainChunkData data, Transform parent)
    {
        Position = position;

        // Set the GameObject
        gameObject.name = "Terrain Chunk " + Position.ToString();
        gameObject.transform.parent = parent;

        UpdateChunkData(data);
    }

    public void UpdateChunkData(TerrainChunkData data)
    {
        Data = data;

        SetLODIndex(0);

        // Always use the highest resolution mesh for collisions
        meshCollider.sharedMesh = Data.Meshes[0];
    }

    public void SetLODIndex(int lod)
    {
        //Debug.Log($"CHUNK {Position.x} {Position.y} LOD {lod}");

        if (lod >= 0 && lod < Data.Meshes.Count)
        {
            meshFilter.sharedMesh = Data.Meshes[lod];

            gameObject.SetActive(true);
            //Debug.Log($"CHUNK {Position.x} {Position.y} LOD {lod}");
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Only enable collisions when the ball is close
        meshCollider.enabled = lod == 0;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size);
    }
}
