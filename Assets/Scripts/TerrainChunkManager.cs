using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour, IManager
{
    public const float ChunkSizeWorldUnits = 1000;

    [Header("References")]
    public Transform ChunkParent;
    public GameObject ChunkPrefab;


    private Dictionary<Vector2Int, TerrainChunk> TerrainChunks = new Dictionary<Vector2Int, TerrainChunk>();


    public TerrainChunk TryAddChunk(TerrainChunkData data)
    {
        if (TerrainChunks.TryGetValue(data.Position, out TerrainChunk c))
        {
            TerrainChunks.Remove(data.Position);
            Destroy(c.gameObject);
        }

        // Create the chunk
        TerrainChunk chunk = Instantiate(ChunkPrefab).GetComponent<TerrainChunk>();
        chunk.Initialise(data.Position, data.Bounds, data, ChunkParent);
        TerrainChunks.Add(data.Position, chunk);
        
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
