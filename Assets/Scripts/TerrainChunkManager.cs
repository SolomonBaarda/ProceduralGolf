using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour
{
    public Grid ChunkGrid;

    public Transform ChunkParent;

    Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


    private void Awake()
    {
        ChunkGrid.cellSize = new Vector3(TerrainChunk.ChunkSizeWorldUnits, TerrainChunk.ChunkSizeWorldUnits);
        ChunkGrid.cellSwizzle = GridLayout.CellSwizzle.XZY;
        // Move the Grid so that chunk 0,0 is centered on the origin
        ChunkGrid.transform.position -= new Vector3(ChunkGrid.cellSize.x / 2, 0, ChunkGrid.cellSize.y / 2);
    }









    public void AddNewChunk(Vector2Int position, TerrainGenerator.HeightMap heightMap, Material terrainMaterial, MeshGenerator.MeshSettings meshSettings)
    {
        if (!TerrainChunks.ContainsKey(position))
        {
            TerrainChunk chunk = new TerrainChunk(position, CalculateTerrainChunkCentreWorld(position), terrainMaterial, ChunkParent, heightMap);

            chunk.UpdateMeshData(MeshGenerator.GenerateMeshData(heightMap, meshSettings, chunk.Bounds));
            chunk.SetVisible(true);

            TerrainChunks.Add(position, chunk);
        }
        else
        {
            Debug.LogError("Chunk " + position.ToString() + " has already been added.");
        }
    }


    public Vector3 CalculateTerrainChunkCentreWorld(Vector2Int chunk)
    {
        return ChunkGrid.GetCellCenterWorld(new Vector3Int(chunk.x, chunk.y, 0));
    }


    public Bounds CalculateTerrainChunkBounds(Vector2Int chunk)
    {
        return new Bounds(CalculateTerrainChunkCentreWorld(chunk), ChunkGrid.cellSize);
    }



    public void Clear()
    {
        // Clear all the chunks
        for (int i = 0; i < ChunkParent.childCount; i++)
        {
            Destroy(ChunkParent.GetChild(i).gameObject);
        }
        TerrainChunks.Clear();
    }




}
