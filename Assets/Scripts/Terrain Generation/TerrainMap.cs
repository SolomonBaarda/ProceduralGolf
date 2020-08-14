using UnityEngine;
using System.Collections.Generic;

public class TerrainMap
{
    public int Width, Height;

    /// <summary>
    /// The maximum LOD Terrain data.
    /// </summary>
    public Point[,] Map;
    public Bounds Bounds;


    public List<Point.NeighbourDirection> EdgeNeighboursAdded;

    public TerrainMap(int width, int height, Bounds bounds)
    {
        Width = width;
        Height = height;

        Bounds = bounds;

        Map = new Point[Width, Height];

        EdgeNeighboursAdded = new List<Point.NeighbourDirection>();
    }



    private static Point.NeighbourDirection CalculateNeighbourDirection(int dirX, int dirY)
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


    public static void AddEdgeNeighbours(int dirX, int dirY, ref TerrainMap map, in TerrainMap neighbour, out bool mapNeedsUpdating)
    {
        dirX = Mathf.Clamp(dirX, -1, 1);
        dirY = Mathf.Clamp(dirY, -1, 1);

        Point.NeighbourDirection direction = CalculateNeighbourDirection(dirX, dirY);

        mapNeedsUpdating = false;

        if (neighbour.Width == map.Width && neighbour.Height == map.Height)
        {
            // Only add the neighbours if it has not been done already
            if (!map.EdgeNeighboursAdded.Contains(direction))
            {
                map.EdgeNeighboursAdded.Add(direction);

                // Horizontal case
                if (direction == Point.NeighbourDirection.Up || direction == Point.NeighbourDirection.Down)
                {
                    int y = 0, neighbourY = map.Height - 1;
                    if (direction == Point.NeighbourDirection.Down)
                    {
                        y = map.Height - 1;
                        neighbourY = 0;
                    }

                    for (int x = 0; x < map.Width; x++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            if (Utils.IsWithinArrayBounds(x + i, neighbourY, map.Map))
                            {
                                AddNeighbourForEdge(ref map.Map[x, y], ref map.Map[x + i, neighbourY], out bool needsUpdating);
                                mapNeedsUpdating |= needsUpdating;
                            }

                        }
                    }
                }
                // Vertical case
                else if (direction == Point.NeighbourDirection.Left || direction == Point.NeighbourDirection.Right)
                {
                    int x = 0, neighbourX = map.Width - 1;
                    if (direction == Point.NeighbourDirection.Right)
                    {
                        x = map.Width - 1;
                        neighbourX = 0;
                    }

                    for (int y = 0; y < map.Height; y++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (Utils.IsWithinArrayBounds(neighbourX, y + j, map.Map))
                            {
                                AddNeighbourForEdge(ref map.Map[x, y], ref map.Map[neighbourX, y + j], out bool needsUpdating);
                                mapNeedsUpdating |= needsUpdating;
                            }

                        }
                    }
                }


                // TODO diaganols 
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
                if (p.Biome == TerrainSettings.Biome.Hole && neighbour.Biome == TerrainSettings.Biome.Hole)
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







    public struct Point
    {
        public const float Empty = 0f;

        public Vector3 LocalVertexBasePosition;

        // Calculate the point of the vertex
        public Vector3 LocalVertexPosition => LocalVertexBasePosition + (TerrainGenerator.UP * Height);

        public Vector3 Offset;


        public bool IsAtEdgeOfMesh;

        public TerrainSettings.Biome Biome;
        public float Height;
        public float OriginalHeight;

        /// <summary>
        /// If this point is part of a Hole.
        /// </summary>
        public Hole Hole;
        public List<Point> Neighbours;


        public Point(Vector3 localVertex, Vector3 offset, float height, TerrainSettings.Biome biome, bool isAtEdgeOfMesh)
        {
            LocalVertexBasePosition = localVertex;
            Offset = offset;

            IsAtEdgeOfMesh = isAtEdgeOfMesh;

            Neighbours = new List<Point>();
            Hole = null;

            Biome = biome;

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




