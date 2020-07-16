using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour
{
    public Grid grid;

    Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


    private void Awake()
    {
        grid.cellSize = new Vector3(TerrainChunk.ChunkSizeWorldUnits, TerrainChunk.ChunkSizeWorldUnits);
    }









    public void AddNewChunk(Vector2Int position, float[,] heightMap)
    {

    }




    public Bounds CalculateTerrainChunkBounds(Vector2Int chunk)
    {
        Vector2 centre = grid.GetCellCenterWorld(new Vector3Int(chunk.x, chunk.y, 0));
        return new Bounds(centre, grid.cellSize);
    }



    public void Clear()
    {
        // Clear all the chunks
        foreach (TerrainChunk c in TerrainChunks.Values)
        {
            c.Clear();
        }
        TerrainChunks.Clear();
    }




}
