using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    public Vector2Int Position { get; private set; }
    public Bounds Bounds { get; private set; }

    public bool IsVisible => gameObject.activeSelf;


    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;


    public TerrainChunkData Data;

    public Mesh MainMesh => Data.MainMesh;
    public Texture2D BiomeColourMap => Data.BiomeColourMap;
    public Biome.Type[,] Biomes;



    public void Initialise(Vector2Int position, Bounds bounds, TerrainChunkData data, Material material, PhysicMaterial physics, Transform parent, int terrainLayer)
    {
        Position = position;
        Bounds = bounds;

        // Set the GameObject
        gameObject.name = "Terrain Chunk " + Position.ToString();
        gameObject.layer = terrainLayer;
        // Terrain chunk must be offset by -chunksize/2 as mesh values need to be offset by a positive value
        gameObject.transform.position = bounds.min;
        gameObject.transform.parent = parent;

        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        // Physics material
        meshCollider.material = physics;

        UpdateChunkData(data);

        // Set an instance of the material
        meshRenderer.material = material;
        // And apply the textures to it

        Vector2 textureTiling = new Vector2(data.Width - 1, data.Height - 1);
        TextureSettings.ApplyToMaterial(meshRenderer.material, BiomeColourMap, textureTiling);
    }




    public void UpdateChunkData(TerrainChunkData data)
    {
        Data = data;
        Biomes = Utils.UnFlatten(data.Biomes, data.Width, data.Height);

        meshFilter.sharedMesh = MainMesh;
        meshCollider.sharedMesh = MainMesh;

        meshCollider.convex = false;
    }






    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Bounds.center, Bounds.size);
    }
}
