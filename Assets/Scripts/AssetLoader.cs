using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class AssetLoader
{
    public const string DefaultWorldSavePath = "Assets/World Saves/";


    private static string WorldSavePath(int seed)
    {
        return DefaultWorldSavePath + seed;
    }


    public static void SaveTerrain(TerrainData data)
    {
        string path = WorldSavePath(data.Seed);
        AssetDatabase.DeleteAsset(path);

        // Create the asset and add all of the chunks
        AssetDatabase.CreateAsset(data, path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Saved terrain data to " + path);
    }


    public static bool TryLoadTerrain(int seed, out TerrainData data)
    {
        string path = WorldSavePath(seed);

        // Try and load the data
        data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);

        bool dataIsValid = data != null;

        Debug.Log("Loaded data from " + path + " (" + dataIsValid + ")");

        return dataIsValid;
    }





}
