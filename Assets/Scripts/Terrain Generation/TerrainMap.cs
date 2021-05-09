using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class TerrainMap
{
    public Vector2Int Chunk;
    public int Width, Height;
    public Bounds Bounds;

    /// <summary>
    /// The maximum LOD Terrain data.
    /// </summary>
    public float[,] Heights;
    public Biome.Type[,] Biomes;
    public Vector3[,] BaseVertices;

    public List<Biome.Decoration>[,] Decoration;

    public TerrainMap(Vector2Int chunk, int width, int height, in Vector3[,] baseVertices, Bounds bounds,
        in float[,] heightsBeforeHole, Biome.Type[,] biomes, List<Biome.Decoration>[,] decoration)
    {
        Chunk = chunk;
        Width = width;
        Height = height;
        Bounds = bounds;

        Heights = heightsBeforeHole;
        BaseVertices = baseVertices;
        Biomes = biomes;
        Decoration = decoration;
    }

    public Vector3 CalculateLocalVertexPosition(int x, int y)
    {
        return BaseVertices[x, y] + (TerrainManager.UP * Heights[x,y]);
    }


    public void GetMinMaxHeight(out float min, out float max)
    {
        min = Heights[0, 0];
        max = min;
        foreach (float f in Heights)
        {
            if (f < min)
                min = f;
            if (f > max)
                max = f;
        }
    }




}




