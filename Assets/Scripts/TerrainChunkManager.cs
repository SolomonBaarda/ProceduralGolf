using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour
{
    [Min(1)]
    public float ChunkSizeWorldUnits = 4;

    [Header("References")]
    public Grid ChunkGrid;
    public Transform ChunkParent;

    private Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


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




    public void AddNewChunk(Vector2Int position, HeightMapGenerator.HeightMap heightMap, Material material, PhysicMaterial physics,
        int terrainLayer, MeshSettings meshSettingsVisual, MeshSettings meshSettingsCollider, bool useSameMesh)
    {
        if (!TerrainChunkExists(position))
        {
            Bounds newChunkBounds = CalculateTerrainChunkBounds(position);
            TerrainChunk chunk = new TerrainChunk(position, newChunkBounds, material, physics, ChunkParent, terrainLayer,
                MeshGenerator.GenerateMeshData(heightMap, newChunkBounds.center));

            chunk.UpdateVisualMesh(meshSettingsVisual);
            chunk.UpdateColliderMesh(meshSettingsCollider, useSameMesh);
            chunk.SetVisible(true);

            TerrainChunks.Add(position, chunk);
        }
        else
        {
            Debug.LogError("Chunk " + position.ToString() + " has already been added.");
        }
    }


    public TerrainChunk GetChunk(Vector2Int chunk)
    {
        TerrainChunks.TryGetValue(chunk, out TerrainChunk val);
        return val;
    }


    public void SetVisibleChunks(List<Vector2Int> visible)
    {
        // Disabel all chunks first
        foreach (TerrainChunk c in TerrainChunks.Values)
        {
            c.SetVisible(false);
        }

        // Then enable the ones we want
        foreach (Vector2Int key in visible)
        {
            if (TerrainChunks.TryGetValue(key, out TerrainChunk chunk))
            {
                chunk.SetVisible(true);
            }
        }
    }


    public bool TerrainChunkIsVisible(Vector2Int chunk)
    {
        if (TerrainChunkExists(chunk))
        {
            TerrainChunks.TryGetValue(chunk, out TerrainChunk c);
            return c.IsVisible;
        }
        return false;
    }


    public bool TerrainChunkExists(Vector2Int chunk)
    {
        return TerrainChunks.ContainsKey(chunk);
    }


    public Vector3 CalculateTerrainChunkCentreWorld(Vector2Int chunk)
    {
        return ChunkGrid.GetCellCenterWorld(new Vector3Int(chunk.x, chunk.y, 0));
    }


    public Bounds CalculateTerrainChunkBounds(Vector2Int chunk)
    {
        return ChunkGrid.GetBoundsLocal(new Vector3Int(chunk.x, chunk.y, 0));
    }


    public Vector2Int WorldToChunk(Vector3 worldPos)
    {
        Vector3Int chunk = ChunkGrid.WorldToCell(worldPos);
        return new Vector2Int(chunk.x, chunk.y);
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




}
