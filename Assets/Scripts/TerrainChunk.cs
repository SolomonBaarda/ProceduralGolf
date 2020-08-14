using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TerrainChunk
{
    public Vector2Int Position { get; }
    public Bounds Bounds { get; }

    private GameObject ChunkParent;
    private Dictionary<TerrainSettings.Biome, Layer> Layers = new Dictionary<TerrainSettings.Biome, Layer>();

    public bool IsVisible => ChunkParent.activeSelf;


    public MeshGenerator.MeshData MeshData;


    public TerrainMap TerrainMap;

    public Texture2D Texture;

    private Material m;
    private PhysicMaterial p;


    public TerrainChunk(Vector2Int position, Bounds bounds, Material material, PhysicMaterial physics, Transform parent, int terrainLayer,
            TerrainMap terrainMap, TextureSettings mapSettings)
    {
        Position = position;
        Bounds = bounds;

        // Set the GameObject
        ChunkParent = new GameObject("Terrain Chunk " + position.ToString())
        {
            // Set position
            layer = terrainLayer
        };

        ChunkParent.transform.position = Bounds.center;
        ChunkParent.transform.parent = parent;
        SetVisible(false);

        // Set the maps
        TerrainMap = terrainMap;


        m = material;
        p = physics;


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




    private Layer GetLayer(TerrainSettings.Biome biome)
    {
        if (!Layers.TryGetValue(biome, out Layer layer))
        {
            layer = new Layer(biome, ChunkParent.transform.position, ChunkParent.transform, m, p);
        }

        return layer;
    }


    private void RecalculateMesh(object meshDataObject)
    {
        MeshData = (MeshGenerator.MeshData)meshDataObject;

        // Add all the sub meshes
        foreach (TerrainSettings.Biome b in MeshData.Meshes.Keys)
        {
            if (MeshData.Meshes.TryGetValue(b, out MeshGenerator.LevelOfDetail LOD))
            {
                // Create the new mesh
                Mesh m = LOD.GenerateMesh();

                // Get the correct game object for this biome
                Layer l = GetLayer(b);

                // And set it
                l.meshCollider.sharedMesh = m;
                l.meshFilter.mesh = m;
            }
        }

        SetVisible(true);
    }


    private MeshGenerator.MeshData GenerateLOD(MeshSettings settings)
    {
        // Create new mesh data
        MeshGenerator.MeshData m = MeshGenerator.GenerateMeshData(TerrainMap);

        m.RecalculateAllLODs(settings);

        // And new level of detail
        return m;
    }



    public void SetVisible(bool visible)
    {
        ChunkParent.SetActive(visible);
    }



    private class Layer
    {
        public GameObject Object;

        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;

        public Layer(TerrainSettings.Biome biome, Vector3 position, Transform parent, Material material, PhysicMaterial physics)
        {
            Object = new GameObject(biome.ToString());
            Object.transform.parent = parent;
            Object.transform.position = position;

            // Add the components
            meshRenderer = Object.AddComponent<MeshRenderer>();
            meshFilter = Object.AddComponent<MeshFilter>();
            meshCollider = Object.AddComponent<MeshCollider>();

            // Material stuff
            meshRenderer.material = material;

            // Physics material
            meshCollider.material = physics;
        }
    }


}
