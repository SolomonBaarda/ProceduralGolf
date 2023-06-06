using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour, IManager
{
    public const float ChunkSizeWorldUnits = 1000;

    [Header("References")]
    public Transform ChunkParent;

    private Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


    public TerrainChunk TryAddChunk(TerrainChunkData data, Material material, PhysicMaterial physics, int terrainLayer)
    {
        TerrainChunk chunk;

        // Need to create new chunk
        if (!TerrainChunkExists(data.Position))
        {
            // Create the chunk
            chunk = new GameObject().AddComponent<TerrainChunk>();
            chunk.Initialise(data.Position, data.Bounds, data, material, physics, ChunkParent, terrainLayer);
            TerrainChunks.Add(data.Position, chunk);
        }
        // Just need to update some values
        else
        {
            if (TerrainChunks.TryGetValue(data.Position, out TerrainChunk c))
            {
                // Update the chunk data 
                c.UpdateChunkData(data);
            }
            chunk = c;
        }

        return chunk;
    }

    public IEnumerable<TerrainChunk> GetAllChunks()
    {
        return TerrainChunks.Values;
    }

    public List<TerrainChunk> GetChunks(List<Vector2Int> chunks)
    {
        List<TerrainChunk> values = new List<TerrainChunk>();
        foreach (Vector2Int key in chunks)
        {
            if (TerrainChunks.TryGetValue(key, out TerrainChunk val))
            {
                values.Add(val);
            }
        }

        return values;
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


    public static Vector3 CalculateTerrainChunkCentreWorld(Vector2Int chunk)
    {
        const float half = ChunkSizeWorldUnits / 2;
        return new Vector3((chunk.x * ChunkSizeWorldUnits) + half, 0, (chunk.y * ChunkSizeWorldUnits) + half);
    }


    public static Bounds CalculateTerrainChunkBounds(Vector2Int chunk)
    {
        return new Bounds(CalculateTerrainChunkCentreWorld(chunk), new Vector3(ChunkSizeWorldUnits, 0, ChunkSizeWorldUnits));
    }

    public static Vector2Int WorldToChunk(Vector3 worldPos)
    {
        float rawX = worldPos.x / ChunkSizeWorldUnits, rawY = worldPos.z / ChunkSizeWorldUnits;

        // Negative positions must be shifted by -1 chunks direction
        int chunkX = rawX < 0 ? (int)rawX - 1 : (int)rawX, chunkY = rawY < 0 ? (int)rawY - 1 : (int)rawY;
        return new Vector2Int(chunkX, chunkY);
    }


    public void Clear()
    {
        // Clear all the chunks
        Utils.DestroyAllChildren(ChunkParent);
        TerrainChunks.Clear();
    }

    public void SetVisible(bool visible)
    {

    }
}
