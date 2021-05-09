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






    private void CheckObjectBeforeInstantiating(TerrainChunkData data, Transform parent)
    {
        // We can just instantiate them all
        if (CurrentLoadedTerrain != null)
        {
            TerrainChunkData current = CurrentLoadedTerrain.Chunks.Find(x => x.X == data.X && x.Y == data.Y);

            if (current != null)
            {
                foreach (WorldObjectData d in data.WorldObjects)
                {
                    WorldObjectData exists = current.WorldObjects.Find(x => x.Prefab.Equals(d.Prefab));

                    foreach (Vector3 pos in d.WorldPositions)
                    {
                        // Only instantiate the prefab if either none of them have been
                        // OR if just that position has not been added
                        if (exists == null || !exists.WorldPositions.Contains(pos))
                        {
                            InstantiateOne(d.Prefab, pos, parent);
                        }
                    }
                }
                return;
            }
        }

        // If we get here, then we can just instantiate them all
        InstantiateAll(data, parent);
    }


    private void InstantiateAll(TerrainChunkData data, Transform parent)
    {
        // Loop through each data
        foreach (WorldObjectData d in data.WorldObjects)
        {
            // And each pos
            foreach (Vector3 pos in d.WorldPositions)
            {
                InstantiateOne(d.Prefab, pos, parent);
            }
        }
    }

    private void InstantiateOne(GameObject g, Vector3 pos, Transform parent)
    {
        Instantiate(g, pos, Quaternion.identity, parent);
    }



    /// <summary>
    /// Load all the chunks.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data, DateTime before)
    {
        StartCoroutine(LoadTerrainAsync(data, before));
    }



    private IEnumerator LoadTerrainAsync(TerrainData data, DateTime before)
    {
        Clear();
        IsLoading = true;

        // Load terrain
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            // Instantiate the terrain
            TerrainChunk c = TerrainChunkManager.TryAddChunk(chunk, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
            // And instantiate all objects
            CheckObjectBeforeInstantiating(chunk, c.transform);


            // Spead it out to one chunk per frame
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
















    public void AddChunks(IEnumerable<TerrainChunkData> chunks)
    {
        foreach (TerrainChunkData d in chunks)
        {
            TerrainChunkManager.TryAddChunk(d, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
        }
    }
















    public static Vector3 CalculateSpawnPoint(float sphereRadius, Vector3 pointOnMesh)
    {
        return pointOnMesh + (UP * sphereRadius);
    }














}
