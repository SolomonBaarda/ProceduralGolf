using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public static readonly Vector3 UP = Vector3.up;
    public static readonly Vector3 ORIGIN = Vector3.zero;

    public TerrainChunkManager TerrainChunkManager;
    public Transform WorldObjectsParent;

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




    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        TerrainChunkManager.Clear();

        // Destroy all of the world objects
        for (int i = 0; i < WorldObjectsParent.childCount; i++)
        {
            Destroy(WorldObjectsParent.GetChild(i).gameObject);
        }

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






    private void CheckObjectBeforeInstantiating(WorldObjectData data)
    {
        // We can just instantiate them all
        if (CurrentLoadedTerrain != null)
        {
            foreach (WorldObjectData d in CurrentLoadedTerrain.WorldObjects)
            {
                // The prefab is already here
                if (data.Prefab.Equals(d.Prefab))
                {
                    foreach (Vector3 pos in data.WorldPositions)
                    {
                        // Instantiate the object if it is not in the list of already added
                        if (!d.WorldPositions.Contains(pos))
                        {
                            InstantiateOne(d.Prefab, pos);
                            d.WorldPositions.Add(pos);
                        }
                    }

                    return;
                }
            }
        }

        // If we get here, then we can just instantiate them all
        InstantiateAll(data);
    }


    private void InstantiateAll(WorldObjectData data)
    {
        foreach (Vector3 pos in data.WorldPositions)
        {
            InstantiateOne(data.Prefab, pos);
        }
    }

    private void InstantiateOne(GameObject g, Vector3 pos)
    {
        Instantiate(g, pos, Quaternion.identity, WorldObjectsParent);
    }



    /// <summary>
    /// Load all the chunks.
    /// </summary>
    /// <param name="data"></param>
    public void LoadTerrain(TerrainData data, DateTime before)
    {
        Clear();

        // Load terrain
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            TerrainChunkManager.TryAddChunk(chunk, MaterialGrass, PhysicsGrass, GroundCheck.GroundLayer);
        }



        // Instantiate all GameObjects
        foreach (WorldObjectData prefab in data.WorldObjects)
        {
            CheckObjectBeforeInstantiating(prefab);
        }







        // Assign the terrain at the end
        HasTerrain = true;
        CurrentLoadedTerrain = data;


        // Debug
        if (before != DateTime.MinValue)
        {
            string message = "* Loaded terrain in " + (DateTime.Now - before).TotalSeconds.ToString("0.0")
                + " seconds with " + data.Chunks.Count + " chunks and " + data.GolfHoles.Count + " holes.";

            Debug.Log(message);
        }
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















    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (CurrentLoadedTerrain != null)
        {
            foreach (WorldObjectData d in CurrentLoadedTerrain.WorldObjects)
            {
                foreach (Vector3 pos in d.WorldPositions)
                {
                    Gizmos.DrawLine(pos, pos + (UP * 100));
                }
            }
        }

    }
}
