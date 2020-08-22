using System;
using UnityEngine;


[Serializable]
public class TerrainChunkDataStorage
{
    public TerrainMap TerrainMap;
    public Texture2D BiomeColourMap;
    public Mesh MainMesh;

    public TerrainChunkDataStorage(TerrainMap m, Texture2D colourMap, Mesh main)
    {
        TerrainMap = m;
        BiomeColourMap = colourMap;
        MainMesh = main;
    }
}
