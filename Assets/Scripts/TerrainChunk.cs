using UnityEngine;

public class TerrainChunk
{
    private Vector2Int position;
    public Bounds Bounds { get; }

    private GameObject meshObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;


    private MeshGenerator.MeshData meshData;

    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, PhysicMaterial physics, Transform parent, int terrainLayer, MeshGenerator.MeshData data)
    {
        this.position = position;
        Bounds = bounds;


        // Set the GameObject
        meshObject = new GameObject("Terrain Chunk " + position.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;
        meshCollider.material = physics;

        meshObject.layer = terrainLayer;

        meshObject.transform.position = Bounds.center;
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Set the height map
        meshData = data;
    }


    public void UpdateVisualMesh(MeshSettings visual)
    {
        meshFilter.mesh = meshData.GenerateMesh(visual);
    }


    public void UpdateColliderMesh(MeshSettings collider)
    {
        meshCollider.sharedMesh = meshData.GenerateMesh(collider);
    }


    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
}
