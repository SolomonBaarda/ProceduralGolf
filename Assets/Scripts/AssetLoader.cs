using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AssetLoader
{
    public const string DefaultWorldSavePath = "World Saves";
    public const string FullWorldSavePath = "Resources/World Saves";

    public const string asset = ".asset";



    public static List<TerrainData> GetAllWorldSaves()
    {
        // Get all of the assets
        List<TerrainData> worlds = new List<TerrainData>(Resources.LoadAll<TerrainData>("World Saves"));

        //TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/Resources/World Saves/0.asset");

        Debug.Log("Loaded " + worlds.Count + " TerrainData assets from file.");
        foreach(TerrainData d in worlds)
        {
            if(d.Chunks == null)
            {
                Debug.Log("chunks null");
            }
            if(d.GolfHoles == null)
            {
                Debug.Log("holes null");
            }
        }

        return worlds;
    }


    public static void SaveTerrain(TerrainData data)
    {
        string path = "Assets/Resources/World Saves/" + data.Seed + asset;
        AssetDatabase.DeleteAsset(path);

        // Create the asset and add all of the chunks
        AssetDatabase.CreateAsset(data, path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Saved terrain data to " + path);
    }


    public static bool TryLoadTerrain(int seed, out TerrainData data)
    {
        string path = "";

        // Try and load the data
        data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);

        return data != null;
    }





}
