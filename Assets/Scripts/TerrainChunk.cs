﻿using System;
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
    public Vector3[,] AllVertices;


    [Header("Gizmos settings")]
    public bool ShowGizmos = true;
    [Range(0, 5)] public float Length = 2;


    public void Initialise(Vector2Int position, Bounds bounds, TerrainChunkData data, Material material, PhysicMaterial physics, Transform parent, int terrainLayer)
    {
        Position = position;
        Bounds = bounds;

        // Set the GameObject
        gameObject.name = "Terrain Chunk " + Position.ToString();
        gameObject.layer = terrainLayer;
        gameObject.transform.position = Bounds.center;
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
        Vector2 textureTiling = new Vector2(Biomes.GetLength(0) - 1, Biomes.GetLength(1) - 1);
        TextureSettings.ApplyToMaterial(meshRenderer.material, BiomeColourMap, textureTiling);
    }




    public void UpdateChunkData(TerrainChunkData data)
    {
        Data = data;

        // Un-flatten the array of biome data so that we can use it
        Biomes = Utils.UnFlatten(data.BiomesFlat, data.Width, data.Height);
        AllVertices = Utils.UnFlatten(data.AllVerticesFlat, data.Width, data.Height);

        meshFilter.sharedMesh = MainMesh;
        meshCollider.sharedMesh = MainMesh;
    }






    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }




    private void OnDrawGizmosSelected()
    {
        if (Biomes != null && AllVertices != null)
        {
            if (ShowGizmos)
            {
                for (int y = 0; y < Data.Height; y++)
                {
                    for (int x = 0; x < Data.Width; x++)
                    {
                        Color c = Color.black;
                        switch (Biomes[x, y])
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
                        Gizmos.DrawLine(AllVertices[x, y], AllVertices[x, y] + (TerrainManager.UP * Length));
                    }
                }
            }
        }
    }




}
