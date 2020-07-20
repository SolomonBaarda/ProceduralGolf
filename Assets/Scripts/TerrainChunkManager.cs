using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class TerrainChunkManager : MonoBehaviour
{
    [Min(1)]
    public float ChunkSizeWorldUnits = 4;

    [Header("References")]
    public Grid ChunkGrid;

    public Transform ChunkParent;

    Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


    private void Awake()
    {
        UpdateGrid();
    }



    private void UpdateGrid()
    {
        ChunkGrid.cellSize = new Vector3(ChunkSizeWorldUnits, ChunkSizeWorldUnits);
        ChunkGrid.cellSwizzle = GridLayout.CellSwizzle.XZY;
        // Move the Grid so that chunk 0,0 is centered on the origin
        ChunkGrid.transform.position = -new Vector3(ChunkGrid.cellSize.x / 2, 0, ChunkGrid.cellSize.y / 2);
    }




    public void AddNewChunk(Vector2Int position, HeightMapGenerator.HeightMap heightMap, Material material, PhysicMaterial physics, MeshGenerator.MeshSettings meshSettingsVisual, MeshGenerator.MeshSettings meshSettingsCollider)
    {
        if (!TerrainChunks.ContainsKey(position))
        {
            TerrainChunk chunk = new TerrainChunk(position, CalculateTerrainChunkBounds(position), material, physics, ChunkParent, MeshGenerator.GenerateMeshData(heightMap));

            chunk.UpdateVisualMesh(meshSettingsVisual);
            chunk.UpdateColliderMesh(meshSettingsCollider);
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
        return ChunkGrid.GetBoundsLocal(new Vector3Int(chunk.x, chunk.y, 0));
    }



    public void Clear()
    {
        // Clear all the chunks
        for (int i = 0; i < ChunkParent.childCount; i++)
        {
            Destroy(ChunkParent.GetChild(i).gameObject);
        }
        TerrainChunks.Clear();
        UpdateGrid();
    }


    /*

    private void OnDrawGizmosSelected()
    {
        foreach (TerrainChunk c in TerrainChunks.Values)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawCube(c.Bounds.center, c.Bounds.size);
        }
    }
    */

}
