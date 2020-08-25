using System;
using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(TerrainManager))]
public class TerrainSaver : Editor
{
    public const string DefaultWorldSavePath = "Assets/World Saves";


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainManager m = (TerrainManager)target;

        if (m.CurrentLoadedTerrain != null)
        {
            if (GUILayout.Button("Save terrain to file"))
            {
                // Save the currently used TerrainData to file
                SaveTerrain(m.CurrentLoadedTerrain);
            }
        }

    }








    private static string FolderPath(int seed)
    {
        return DefaultWorldSavePath + "/" + seed;
    }

    private static string Chunk(int x, int y)
    {
        return "/(" + x + "," + y + ") ";
    }



    public static void SaveTerrain(TerrainData data)
    {
        DateTime before = DateTime.Now;

        string folder = FolderPath(data.Seed);
        string path = folder + "/" + data.Seed + ".asset";

        AssetDatabase.DeleteAsset(folder);
        AssetDatabase.CreateFolder(DefaultWorldSavePath, data.Seed.ToString());

        // Create the asset and add all of the chunks
        AssetDatabase.CreateAsset(data, path);

        foreach (TerrainChunkData d in data.Chunks)
        {
            string chunkPath = folder + Chunk(d.X, d.Y);

            string texturePath = chunkPath + "texture.asset";
            AssetDatabase.CreateAsset(d.BiomeColourMap, texturePath);

            string meshPath = chunkPath + "mesh.asset";
            AssetDatabase.CreateAsset(d.MainMesh, meshPath);
        }

        UpdateReferences(ref data);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("* Saved terrain data in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds to " + path);
    }




    public static bool UpdateReferences(ref TerrainData data)
    {
        string folderPath = FolderPath(data.Seed);

        // Assign the texture and mesh
        foreach (TerrainChunkData chunk in data.Chunks)
        {
            string c = Chunk(chunk.X, chunk.Y);
            chunk.BiomeColourMap = AssetDatabase.LoadAssetAtPath<Texture2D>(folderPath + "/" + c + "texture.asset");
            chunk.MainMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folderPath + "/" + c + "mesh.asset");
        }

        return data != null;
    }





}
