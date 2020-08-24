using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;

    public bool HasTerrain { get; private set; }

    [Header("Materials")]
    public Material MaterialGrass;

    [Header("Physics")]
    public PhysicMaterial PhysicsGrass;

    [Header("Prefabs")]
    public GameObject GolfHoleFlagPrefab;
    public GameObject GolfHolePositionPrefab;

    [Space]
    public TerrainData CurrentLoadedTerrain;


    [Space]
    private Transform Player;
    private bool HideChunks = true;
    private float ViewDistance = 0;




    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        TerrainChunkManager.Clear();
        HasTerrain = false;

        CurrentLoadedTerrain = null;
    }

    public void Set(bool hideChunks, Transform player, float viewDistance)
    {
        HideChunks = hideChunks;
        Player = player;
        ViewDistance = viewDistance;
    }





    private void Update()
    {
        if (HideChunks && Player != null && ViewDistance > 0)
        {
            foreach (TerrainChunk chunk in TerrainChunkManager.GetAllChunks())
            {
                //chunk.SetVisible(Vector3.Distance(transform.position, chunk.Bounds.center) <= ViewDistance);
                chunk.SetVisible(chunk.Bounds.center.sqrMagnitude - transform.position.sqrMagnitude <= ViewDistance * ViewDistance);
            }
        }
    }







    /// <summary>
    /// Load all the chunks.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data)
    {
        Clear();

        HasTerrain = true;
        CurrentLoadedTerrain = data;

        foreach (TerrainChunkData chunk in data.Chunks)
        {
            TerrainChunkManager.TryAddChunk(chunk, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
        }

    }


    public void AddChunks(IEnumerable<TerrainChunkData> chunks)
    {
        foreach (TerrainChunkData d in chunks)
        {
            TerrainChunkManager.TryAddChunk(d, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
        }
    }

}
