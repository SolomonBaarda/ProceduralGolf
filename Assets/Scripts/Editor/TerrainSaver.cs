﻿using System;
using UnityEditor;
using UnityEngine;


public static class TerrainSaver
{
    public const string DefaultWorldSavePath = "Assets/World Saves";


    private static string FolderPath(TerrainData data)
    {
        return DefaultWorldSavePath + "/" + WorldSaveName(data);
    }

    private static string WorldSaveName(TerrainData data)
    {
        return data.TerrainSettingsName + data.Seed;
    }


    private static string Chunk(int x, int y)
    {
        return "/(" + x + "," + y + ") ";
    }


    public static void SaveTerrain(TerrainData data)
    {
        DateTime before = DateTime.Now;

        string folder = FolderPath(data);
        string path = folder + "/" + WorldSaveName(data) + ".asset";

        AssetDatabase.DeleteAsset(folder);
        AssetDatabase.CreateFolder(DefaultWorldSavePath, WorldSaveName(data));

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

        Logger.Log("* Saved terrain data in " + (DateTime.Now - before).TotalSeconds.ToString("0.0") + " seconds to " + path);
    }




    public static bool UpdateReferences(ref TerrainData data)
    {
        string folderPath = FolderPath(data);

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
