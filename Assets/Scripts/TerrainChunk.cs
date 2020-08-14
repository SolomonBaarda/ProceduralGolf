using UnityEngine;
using UnityEngine.UI;

public class TerrainChunk
{
    public Vector2Int Position { get; }
    public Bounds Bounds { get; }

    private GameObject meshObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public Mesh Visual => meshFilter.mesh;
    public Mesh Collider => meshCollider.sharedMesh;

    public bool IsVisible => meshObject.activeSelf;


    public MeshGenerator.MeshData MeshData;
    private MeshGenerator.LevelOfDetail LOD;


    public TerrainMap TerrainMap;

    public Texture2D Texture;


    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, PhysicMaterial physics, Transform parent, int terrainLayer,
            TerrainMap terrainMap, TextureSettings mapSettings)
    {
        Position = position;
        Bounds = bounds;

        // Set the GameObject
        meshObject = new GameObject("Terrain Chunk " + position.ToString());
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        // Material stuff
        meshRenderer.material = material;


        // Physics material
        meshCollider.material = physics;

        // Set position
        meshObject.layer = terrainLayer;
        meshObject.transform.position = Bounds.center;
        meshObject.transform.parent = parent;
        SetVisible(false);

        // Set the maps
        TerrainMap = terrainMap;


        RecalculateTexture(mapSettings);
        //meshRenderer.material.SetTexture("_BaseMap", Texture);
    }



    public void RecalculateTexture(TextureSettings mapSettings)
    {
        // Request the texture and set it afterwards
        ThreadedDataRequester.RequestData(() => TextureGenerator.GenerateTextureData(TerrainMap, mapSettings), SetTexture);
    }


    private void SetTexture(object textureDataObject)
    {
        TextureGenerator.TextureData data = (TextureGenerator.TextureData)textureDataObject;

        // Create the texture from the data
        Texture = TextureGenerator.GenerateTexture(data);
    }



    public void RecalculateMesh(MeshSettings settings)
    {
        SetVisible(false);

        // Recalculate all the new mesh data then create a new mesh
        ThreadedDataRequester.RequestData(() => GenerateLOD(settings), RecalculateMesh);
    }


    private void RecalculateMesh(object LODObject)
    {
        LOD = (MeshGenerator.LevelOfDetail)LODObject;

        // Create the new mesh
        Mesh mesh = LOD.GenerateMesh();
        // And set it
        meshCollider.sharedMesh = mesh;
        meshFilter.mesh = mesh;

        SetVisible(true);
    }


    private MeshGenerator.LevelOfDetail GenerateLOD(MeshSettings settings)
    {
        // Create new mesh data
        MeshData = MeshGenerator.GenerateMeshData(TerrainMap);
        // And new level of detail
        return MeshData.GenerateLOD(settings);
    }



    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }






}
