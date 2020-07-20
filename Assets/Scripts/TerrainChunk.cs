using UnityEngine;

public class TerrainChunk
{
    Vector2Int position;
    public Bounds Bounds { get; }

    GameObject meshObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    TerrainGenerator.HeightMap heightMap;

    MeshGenerator.MeshData meshData;

    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, Transform parent, TerrainGenerator.HeightMap heightMap)
    {
        this.position = position;
        Bounds = bounds;


        // Set the GameObject
        meshObject = new GameObject("Terrain Chunk " + position.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = Bounds.center;
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Set the height map
        this.heightMap = heightMap;
    }

    public void UpdateMeshData(MeshGenerator.MeshData data)
    {
        meshData = data;

        meshFilter.mesh = meshData.CreateMesh();
    }


    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
}
