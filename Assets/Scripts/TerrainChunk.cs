using UnityEngine;

public class TerrainChunk
{
    public const float ChunkSizeWorldUnits = 1;
    public const int NoiseSamplePointsDensity = 16;

    Vector2Int position;
    public Bounds Bounds { get; }

    GameObject meshObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    HeightMap heightMap;

    public TerrainChunk(Vector2Int position, Vector3 centre, Material material, Transform parent, HeightMap heightMap)
    {
        this.position = position;
        Bounds = new Bounds(centre, new Vector2(ChunkSizeWorldUnits, ChunkSizeWorldUnits));


        // Set the GameObject
        meshObject = new GameObject("Terrain Chunk " + position.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Set the height map
        this.heightMap = heightMap;
    }





    public void Clear()
    {
        Object.Destroy(meshObject);
    }


    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
}
