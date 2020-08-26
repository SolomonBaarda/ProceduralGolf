using UnityEngine;
using System.Collections.Generic;



public class TerrainMap
{
    public Vector2Int Chunk;
    public int Width, Height;
    public Bounds Bounds;

    /// <summary>
    /// The maximum LOD Terrain data.
    /// </summary>
    public Point[,] Map;

    public List<Point.NeighbourDirection> EdgeNeighboursAdded;

    public TerrainMap(Vector2Int chunk, int width, int height, in Vector3[,] baseVertices, Bounds bounds,
        in float[,] heightsBeforeHole, Biome.Type[,] biomes, Biome.Decoration [,]decoration)
    {
        Chunk = chunk;
        Width = width;
        Height = height;
        Bounds = bounds;

        EdgeNeighboursAdded = new List<Point.NeighbourDirection>();

        // Create the map
        Map = new Point[width, height];

        // Assign all the terrain point vertices
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool atEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;

                // Assign the terrain point
                Map[x, y] = new Point(baseVertices[x, y], bounds.center, heightsBeforeHole[x,y], biomes[x,y], decoration[x,y], atEdge);
            }
        }

        // Now set each neighbour
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Point p = Map[x, y];

                // Add the 3x3 of points as neighbours
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        int pointX = x + i, pointY = y + j;

                        // Ensure within the array bounds
                        if (Utils.IsWithinArrayBounds(pointX, pointY, in Map))
                        {
                            // Don't add its self
                            if (pointX != x || pointY != y)
                            {
                                Point neighbour = Map[pointX, pointY];

                                // Add the neighbour
                                p.Neighbours.Add(neighbour);
                            }
                        }
                    }
                }
            }
        }
    }

    private Point.NeighbourDirection CalculateNeighbourDirection(int dirX, int dirY)
    {
        Point.NeighbourDirection d = Point.NeighbourDirection.Up;

        // Sides
        if (dirX != dirY)
        {
            if (dirX == -1)
            {
                d = Point.NeighbourDirection.Left;
            }
            else if (dirX == 1)
            {
                d = Point.NeighbourDirection.Right;
            }
            else if (dirY == -1)
            {
                d = Point.NeighbourDirection.Up;
            }
            else if (dirY == 1)
            {
                d = Point.NeighbourDirection.Down;
            }
        }
        // Corners
        else
        {
            if (dirX == -1 && dirY == -1)
            {
                d = Point.NeighbourDirection.UpLeft;
            }
            else if (dirX == 1 && dirY == -1)
            {
                d = Point.NeighbourDirection.UpRight;
            }
            else if (dirX == -1 && dirY == 1)
            {
                d = Point.NeighbourDirection.DownLeft;
            }
            else if (dirX == 1 && dirY == 1)
            {
                d = Point.NeighbourDirection.DownRight;
            }
        }

        return d;
    }






    public void DebugMinMaxHeight()
    {
        float min = Map[0, 0].Height, max = min;
        foreach (Point p in Map)
        {
            min = p.Height < min ? p.Height : min;
            max = p.Height > max ? p.Height : max;
        }

        Debug.Log("Terrain map " + Chunk.ToString() + "min: " + min + " max: " + max);
    }




    public void AddEdgeNeighbours(int dirX, int dirY, TerrainMap map, out bool mapNeedsUpdating)
    {
        dirX = Mathf.Clamp(dirX, -1, 1);
        dirY = Mathf.Clamp(dirY, -1, 1);

        Point.NeighbourDirection direction = CalculateNeighbourDirection(dirX, dirY);

        mapNeedsUpdating = false;

        if (Width == map.Width && Height == map.Height)
        {
            // Only add the neighbours if it has not been done already
            if (!EdgeNeighboursAdded.Contains(direction))
            {
                EdgeNeighboursAdded.Add(direction);

                // Horizontal case
                if (direction == Point.NeighbourDirection.Up || direction == Point.NeighbourDirection.Down)
                {
                    int y = 0, neighbourY = Height - 1;
                    if (direction == Point.NeighbourDirection.Down)
                    {
                        y = Height - 1;
                        neighbourY = 0;
                    }

                    for (int x = 0; x < Width; x++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            if (Utils.IsWithinArrayBounds(x + i, neighbourY, map.Map))
                            {
                                AddNeighbourForEdge(ref Map[x, y], ref map.Map[x + i, neighbourY], out bool needsUpdating);
                                mapNeedsUpdating |= needsUpdating;
                            }

                        }
                    }
                }
                // Vertical case
                else if (direction == Point.NeighbourDirection.Left || direction == Point.NeighbourDirection.Right)
                {
                    int x = 0, neighbourX = Width - 1;
                    if (direction == Point.NeighbourDirection.Right)
                    {
                        x = Width - 1;
                        neighbourX = 0;
                    }

                    for (int y = 0; y < Height; y++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (Utils.IsWithinArrayBounds(neighbourX, y + j, map.Map))
                            {
                                AddNeighbourForEdge(ref Map[x, y], ref map.Map[neighbourX, y + j], out bool needsUpdating);
                                mapNeedsUpdating |= needsUpdating;
                            }

                        }
                    }
                }

                // Diagonal cases - TODO
                /*

                // Neighbour is diagonal up left
                if (direction == Point.NeighbourDirection.UpLeft)
                {
                    AddNeighbourForEdge(ref Map[0, 0], ref map.Map[Width - 1, Height - 1], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                // Neighbour is diagonal up right
                else if (direction == Point.NeighbourDirection.UpRight)
                {
                    AddNeighbourForEdge(ref Map[Width - 1, 0], ref map.Map[0, Height - 1], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                // Neighbour is diagonal down left
                else if (direction == Point.NeighbourDirection.DownLeft)
                {
                    AddNeighbourForEdge(ref Map[0, Height - 1], ref map.Map[Width - 1, 0], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                // Neighbour is diagonal down right
                else if (direction == Point.NeighbourDirection.DownLeft)
                {
                    AddNeighbourForEdge(ref Map[Width - 1, Height - 1], ref map.Map[0, 0], out bool updated);
                    mapNeedsUpdating |= updated;
                }
                */

            }
        }
        else
        {
            Debug.LogError("Trying to add edge neighbours for TerrainMaps of different size.");
        }
    }


    private static void AddNeighbourForEdge(ref Point p, ref Point neighbour, out bool terrainNeedsUpdating)
    {
        terrainNeedsUpdating = false;
        if (p.IsAtEdgeOfMesh && neighbour.IsAtEdgeOfMesh)
        {
            if (!p.Neighbours.Contains(neighbour))
            {
                p.Neighbours.Add(neighbour);

                // These neighbours are the same hole that is split by the chunk border
                if (p.Biome == Biome.Type.Hole && neighbour.Biome == Biome.Type.Hole)
                {
                    if (p.Hole != neighbour.Hole)
                    {
                        terrainNeedsUpdating = true;
                        p.Hole.Merge(ref neighbour.Hole);
                    }
                }
            }
        }
    }





    public class Point
    {
        public const float Empty = 0f;

        public Vector3 LocalVertexBasePosition;
        // Calculate the point of the vertex
        public Vector3 LocalVertexPosition => LocalVertexBasePosition + (TerrainManager.UP * Height);
        public Vector3 Offset;

        public bool IsAtEdgeOfMesh;

        public Biome.Type Biome;
        public float Height;
        public float OriginalHeight;

        public Biome.Decoration Decoration;

        /// <summary>
        /// If this point is part of a Hole.
        /// </summary>
        public Hole Hole;
        public List<Point> Neighbours;


        public Point(Vector3 localVertexPos, Vector3 offset, float height, Biome.Type biome, Biome.Decoration decoration, bool isAtEdgeOfMesh)
        {
            LocalVertexBasePosition = localVertexPos;
            Offset = offset;

            IsAtEdgeOfMesh = isAtEdgeOfMesh;

            Neighbours = new List<Point>();

            Biome = biome;
            Decoration = decoration;
            Height = height;
            OriginalHeight = Height;
        }


        public enum NeighbourDirection
        {
            Up, Down, Left, Right,
            UpLeft, UpRight, DownLeft, DownRight,
        }

    }






}




