using System;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    public Vector2Int Position { get; private set; }
    public Bounds Bounds { get; private set; }

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public Mesh Visual => meshFilter.mesh;
    public Mesh Collider => meshCollider.sharedMesh;

    public bool IsVisible => gameObject.activeSelf;


    public MeshGenerator.MeshData MeshData;
    private Mesh MainMesh;


    public TerrainMap TerrainMap;
    [HideInInspector]
    public Texture2D BiomeColourMap;

    [Header("Gizmos settings")]
    public bool ShowGizmos = true;
    [Min(0)] public float Length = 2;
    public bool GizmosUseLOD = true;
    private int LODIncrement = 1;

    public void Initialise(Vector2Int position, Bounds bounds, Material material, PhysicMaterial physics, Transform parent, int terrainLayer,
            TerrainMap terrainMap, TextureSettings textureSettings)
    {
        Position = position;
        Bounds = bounds;

        // Set the GameObject
        gameObject.name = "Terrain Chunk " + position.ToString();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshCollider = gameObject.AddComponent<MeshCollider>();

        // Material stuff
        meshRenderer.material = material;


        // Physics material
        meshCollider.material = physics;

        // Set position
        gameObject.layer = terrainLayer;
        gameObject.transform.position = Bounds.center;
        gameObject.transform.parent = parent;
        SetVisible(false);

        // Set the maps
        TerrainMap = terrainMap;

        RecalculateTexture(textureSettings);
    }







    public void RecalculateTexture(TextureSettings settings)
    {
        // Request the texture and set it afterwards
        ThreadedDataRequester.RequestData(() => TextureGenerator.GenerateTextureData(TerrainMap, settings), SetTexture);
    }


    private void SetTexture(object textureDataObject)
    {
        TextureGenerator.TextureData data = (TextureGenerator.TextureData)textureDataObject;

        // Reverse the colour map - rotates 180 degrees 
        // For some reason the texture needs this
        Array.Reverse(data.ColourMap);

        // Create the texture from the data
        BiomeColourMap = TextureGenerator.GenerateTexture(data);


        Material m = meshRenderer.material;
        Vector2 tiling = new Vector2(TerrainMap.Width - 1, TerrainMap.Height - 1);

        TextureSettings.ApplyToMaterial(ref m, BiomeColourMap, tiling, data.Settings.GetColour(Biome.Type.Grass), data.Settings.GetColour(Biome.Type.Hole), data.Settings.GetColour(Biome.Type.Sand));
    }



    public void RecalculateMesh(MeshSettings settings)
    {
        SetVisible(false);

        LODIncrement = settings.SimplificationIncrement;


        // Request for the mesh data to be updated 
        ThreadedDataRequester.RequestData(() => UpdateMeshData(settings), UpdateMesh);
    }


    private MeshSettings UpdateMeshData(in MeshSettings settings)
    {
        // Update the mesh data
        MeshGenerator.UpdateMeshData(ref MeshData, TerrainMap);
        // Pass on the settings
        return settings;
    }


    private void UpdateMesh(object o)
    {
        MeshSettings settings = (MeshSettings)o;

        // Update the mesh to the new vertex values
        MeshData.UpdateMesh(ref MainMesh, settings);

        // And set it
        meshFilter.sharedMesh = MainMesh;
        meshCollider.sharedMesh = MainMesh;

        SetVisible(true);
    }



    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }




    private void OnDrawGizmosSelected()
    {
        if (TerrainMap != null)
        {
            if (ShowGizmos)
            {
                int i = GizmosUseLOD ? LODIncrement : 1;

                for (int y = 0; y < TerrainMap.Map.GetLength(1); y += i)
                {
                    for (int x = 0; x < TerrainMap.Map.GetLength(0); x += i)
                    {
                        TerrainMap.Point p = TerrainMap.Map[x, y];

                        Color c = Color.black;
                        switch (p.Biome)
                        {
                            case Biome.Type.Grass:
                                c = Color.green;
                                break;
                            case Biome.Type.Sand:
                                c = Color.yellow;
                                break;
                            case Biome.Type.Hole:
                                c = Color.gray;
                                break;
                            case Biome.Type.Water:
                                c = Color.blue;
                                break;
                            case Biome.Type.Ice:
                                c = Color.white;
                                break;
                        }

                        Gizmos.color = c;
                        Vector3 pos = p.LocalVertexPosition + TerrainMap.Bounds.center;
                        Gizmos.DrawLine(pos, pos + (TerrainGenerator.UP * Length));
                    }
                }
            }
        }
    }

}
