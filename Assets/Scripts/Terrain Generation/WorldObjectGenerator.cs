using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldObjectGenerator : MonoBehaviour
{
    public float Radius = 20;
    [Range(1, 10)]
    public int Iterations = 5;


    [Space]
    public List<WorldObjectPreset> WorldObjectPrefabs;
    private Dictionary<Biome.Decoration, List<GameObject>> Prefabs = new Dictionary<Biome.Decoration, List<GameObject>>();



    private void Awake()
    {
        // Add all the objects to the dictionary
        foreach (WorldObjectPreset p in WorldObjectPrefabs)
        {
            // Add a new entry if not there
            if (!Prefabs.TryGetValue(p.Type, out List<GameObject> prefabList))
            {
                prefabList = new List<GameObject>();
                Prefabs.Add(p.Type, prefabList);
            }

            // Add all prefabs to the list
            prefabList.AddRange(p.Prefabs);
        }
    }





    public List<WorldObjectData> CalculateDataForChunk(TerrainMap m)
    {
        Dictionary<GameObject, WorldObjectData> prefabsInChunk = new Dictionary<GameObject, WorldObjectData>();

        int seed = Noise.Seed(m.Chunk.ToString());

        // Get the local position
        List<Vector2> localPosition2D = PoissonDiscSampling.GenerateLocalPoints(Radius, new Vector2(m.Bounds.size.x, m.Bounds.size.z), seed, Iterations);
        //Debug.Log("valid points in chunk:" + localPosition2D.Count);

        System.Random r = new System.Random(seed);

        // Add the offset to it
        Vector3 offset = new Vector3(m.Bounds.min.x, 0, m.Bounds.min.z);
        // Loop through each position
        foreach (Vector2 pos in localPosition2D)
        {
            // Get the correct world pos
            Vector3 worldPos = new Vector3(pos.x, 0, pos.y) + offset;
            TerrainMap.Point p = Utils.GetClosestTo(worldPos, m.Bounds.min, m.Bounds.max, in m.Points);
            worldPos.y = p.LocalVertexPosition.y;

            // Find its type
            Biome.Decoration type = p.Decoration;
            if (type != Biome.Decoration.None)
            {
                if (Prefabs.TryGetValue(type, out List<GameObject> prefabs))
                {
                    if (prefabs.Count > 0)
                    {
                        // Choose the prefab
                        int index = r.Next(0, prefabs.Count);
                        GameObject prefab = prefabs[index];

                        // Create it if we need to
                        if (!prefabsInChunk.TryGetValue(prefab, out WorldObjectData d))
                        {
                            d = new WorldObjectData(prefab, new List<Vector3>());
                            prefabsInChunk.Add(prefab, d);
                        }

                        // Add it
                        d.WorldPositions.Add(worldPos);
                    }
                }
            }
        }


        return prefabsInChunk.Values.ToList();
    }
















    [Serializable]
    public class WorldObjectPreset
    {
        public Biome.Decoration Type;
        public List<GameObject> Prefabs;
    }
}
