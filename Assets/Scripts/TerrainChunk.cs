using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    public Vector2Int Position { get; private set; }
    public Bounds Bounds { get; private set; }

    public bool IsVisible => gameObject.activeSelf;

    private int CurrentLOD = -1;


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
        Bounds = data.Meshes[0].bounds;

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
        if (lod != CurrentLOD)
        {
            // Only enable collisions when the ball is close
            bool collisionsEnabled = lod == 0;
            meshCollider.enabled = collisionsEnabled;

            if (lod >= 0 && lod < Data.Meshes.Count)
            {
                meshFilter.sharedMesh = Data.Meshes[lod];

                gameObject.SetActive(true);

                // Very slow
                foreach (GameObjectLOD g in gameObject.GetComponentsInChildren<GameObjectLOD>())
                {
                    if (lod < g.Meshes.Length)
                    {
                        g.MeshFilter.sharedMesh = g.Meshes[lod];
                    }

                    g.MeshCollider.enabled = collisionsEnabled;
                }
            }
            else
            {
                gameObject.SetActive(false);
            }

            CurrentLOD = lod;
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size);
    }
}
