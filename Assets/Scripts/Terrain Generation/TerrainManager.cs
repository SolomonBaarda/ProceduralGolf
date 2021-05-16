using System;
using System.Collections;
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

    [Space]
    public TerrainData CurrentLoadedTerrain;

    private Transform Player;
    private bool HideChunks = true;
    private float ViewDistance = 0;


    public bool IsLoading { get; private set; } = false;

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



    private void LateUpdate()
    {
        if (HideChunks && Player != null && ViewDistance > 0)
        {
            foreach (TerrainChunk chunk in TerrainChunkManager.GetAllChunks())
            {
                // Only set the chunks within render distance to be visible
                chunk.SetVisible((chunk.Bounds.center - Player.position).sqrMagnitude <= ViewDistance * ViewDistance);
            }
        }
    }




    /// <summary>
    /// Load all the chunks.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data)
    {
        StartCoroutine(LoadTerrainAsync(data));
    }



    private IEnumerator LoadTerrainAsync(TerrainData data)
    {
        DateTime before = DateTime.Now;
        Clear();
        IsLoading = true;

        // Load terrain
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            // Instantiate the terrain
            TerrainChunk c = TerrainChunkManager.TryAddChunk(chunk, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
            // And instantiate all objects

            foreach (WorldObjectData worldObjectData in chunk.WorldObjects)
            {
                foreach (Vector3 worldPosition in worldObjectData.WorldPositions)
                {
                    Instantiate(worldObjectData.Prefab, worldPosition, Quaternion.identity, c.transform);
                }
            }

            // Wait for next frame
            yield return null;
        }


        // Assign the terrain at the end
        HasTerrain = true;
        CurrentLoadedTerrain = data;
        IsLoading = false;


        // Debug
        string message = "* Loaded terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0")
            + " seconds with " + data.Chunks.Count + " chunks and " + data.GolfHoles.Count + " holes.";

        Debug.Log(message);

    }



    public static Vector3 CalculateSpawnPoint(float sphereRadius, Vector3 pointOnMesh)
    {
        return pointOnMesh + (UP * sphereRadius);
    }







}
