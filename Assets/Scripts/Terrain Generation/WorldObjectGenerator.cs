using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldObjectGenerator : MonoBehaviour
{
    public float Radius = 5;
    [Range(1, 10)]
    public int Iterations = 1;


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

            prefabList.Add(p.Prefab);
        }
    }





    public List<WorldObjectData> CalculateDataForChunk(TerrainMap m)
    {
        List<WorldObjectData> data = new List<WorldObjectData>();


        List<Vector3> possibleWorldPositions = PoissonDiscSampling.GenerateWorldPoints(Radius, m.Bounds, Noise.Seed(m.Chunk.ToString()), Iterations);

        if (Prefabs.TryGetValue(Biome.Decoration.Tree, out List<GameObject> l))
        {
            data.Add(new WorldObjectData(l[0], possibleWorldPositions));
        }



        return data;
    }



















    [Serializable]
    public class WorldObjectPreset
    {
        public GameObject Prefab;
        public Biome.Decoration Type;
    }
}
