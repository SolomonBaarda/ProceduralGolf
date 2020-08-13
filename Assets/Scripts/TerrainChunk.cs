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
    public TerrainMap TerrainMap;

    public Texture2D Texture;
    private TextureSettings mapSettings;

    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, PhysicMaterial physics, Transform parent, int terrainLayer,
            MeshGenerator.MeshData data, TerrainMap terrainMap, TextureSettings mapSettings)
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
        MeshData = data;
        TerrainMap = terrainMap;

        this.mapSettings = mapSettings;

        RecalculateTexture();
        //meshRenderer.material.SetTexture("_BaseMap", Texture);
    }



    public void RecalculateTexture()
    {
        Texture = TextureGenerator.GenerateTexture(TerrainMap, mapSettings);
    }


    public void UpdateVisualMesh(MeshSettings visual)
    {
        meshFilter.mesh = MeshData.GenerateMesh(visual);


        /*
        for (int y = 0; y < TerrainMap.Height; y += 1)
        {
            for (int x = 0; x < TerrainMap.Width; x += 1)
            {
                switch (TerrainMap.Map[x, y].Biome)
                {
                    case TerrainSettings.Biome.Grass:
                        break;
                    case TerrainSettings.Biome.Sand:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.yellow, 100);
                        break;
                    case TerrainSettings.Biome.Hole:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.red, 100);
                        break;
                    case TerrainSettings.Biome.Water:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.blue, 100);
                        break;
                    case TerrainSettings.Biome.Ice:
                        Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.white, 100);
                        break;
                }


                if (TerrainMap.Map[x, y].IsAtEdgeOfMesh)
                {
                    //Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.black, 500);

                    if (TerrainMap.Map[x, y].Biome == TerrainSettings.Biome.Hole)
                    {
                        //Debug.DrawRay(Bounds.center + TerrainMap.Map[x, y].LocalVertexPosition, TerrainGenerator.UP, Color.green, 1000);
                    }
                }
            }
        }
        */

    }


    public void UpdateColliderMesh(MeshSettings collider, bool useSameMesh)
    {
        Mesh m = meshFilter.mesh;
        if (!useSameMesh)
        {
            m = MeshData.GenerateMesh(collider);
        }

        meshCollider.sharedMesh = m;
    }





    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }






}
