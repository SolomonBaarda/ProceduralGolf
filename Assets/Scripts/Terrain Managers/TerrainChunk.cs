using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    public Vector2Int Position { get; private set; }
    public Bounds Bounds { get; private set; }

    public bool IsVisible => gameObject.activeSelf;

    [SerializeField]
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

        SetLODIndex(0, true);

        // Always use the highest resolution mesh for collisions
        meshCollider.sharedMesh = Data.Meshes[0];
    }

    public void SetLODIndex(int lod, bool collisionsEnabled)
    {
        if (lod != CurrentLOD || collisionsEnabled != meshCollider.enabled)
        {
            if (lod >= 0 && lod < Data.Meshes.Count)
            {
                meshCollider.enabled = collisionsEnabled;
                meshFilter.sharedMesh = Data.Meshes[lod];

                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }

            CurrentLOD = lod;
        }
    }

    public static Biome.Type GetBiomeSamplePoint(Collider collider, Vector3 worldPos)
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

        return Biome.Type.None;
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size);
    }
}
