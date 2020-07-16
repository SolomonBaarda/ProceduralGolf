using System;
using System.Collections.Generic;
using UnityEngine;

public class HexMap : MonoBehaviour
{
    // Constants
    public const float GridSize = 1.0f;
    public const float HexagonEdgeLength = GridSize / 2;

    public const float HeightMultiplierMinimum = 0f;
    public const float HeightMultiplierMaximum = 1f;

    [Header("Grid display settings")]
    [Min(0)] public float HeightOffset = 25f * GridSize;
    [Min(1)] public int MaxTerraces = 10;
    public bool MakeTerraces = true;

    [Header("Chunk settings")]
    public Transform AllChunksParent;
    public TerrainChunk[] AllChunks => AllChunksParent.GetComponentsInChildren<TerrainChunk>();
    public const int ChunkSizeInHexagons = 64;

    public Dictionary<Vector2Int, TerrainChunk> Chunks { get; private set; } = new Dictionary<Vector2Int, TerrainChunk>();

    [Header("Object references")]
    public Grid Grid;
    public AnimationCurve HeightCurve;

    public float MinHeight { get { return Grid.GetCellCenterWorld(Vector3Int.zero).z - (HeightCurve.Evaluate(0) * HeightOffset); } }
    public float MaxHeight { get { return Grid.GetCellCenterWorld(Vector3Int.zero).z - (HeightCurve.Evaluate(1) * HeightOffset); } }

    [Space]
    public Material GroundMaterial;
    public Material WaterMaterial;


    private void Awake()
    {
        Grid.cellSize = new Vector3(GridSize, GridSize, 0);
    }



    public void ClearAll()
    {
        // Destroy all the meshes
        foreach (TerrainChunk c in Chunks.Values)
        {
            c.ClearAll();
        }

        // Destroy all the children
        for (int i = 0; i < AllChunksParent.childCount; i++)
        {
            Destroy(AllChunksParent.GetChild(i).gameObject);
        }

        // Clear the dictionary
        Chunks.Clear();
    }




    public void RecalculateAll()
    {
        // Recalculate all chunks in the map
        foreach (TerrainChunk c in AllChunks)
        {
            RecalculateChunk(c);
        }
    }


    public void RecalculateChunk(TerrainChunk chunk)
    {
        // Recalculate each edge for all the hexagons in this chunk
        foreach (Hex h in chunk.Hexagons.Values)
        {
            RecalculateEdges(h);
        }

        // Merge all the meshes
        chunk.MergeAllMeshes(MakeTerraces);
    }


    private void RecalculateEdges(Hex h)
    {
        // Get all neighbour hexagons
        List<Hex> neighbours = new List<Hex>();
        foreach (Vector3Int neighbourCell in Hex.CalculatePossibleNeighbourCells(h.Cell))
        {
            // Get the valid chunk
            Hex neighbour = FindHex(neighbourCell);

            if (neighbour != null)
            {
                neighbours.Add(neighbour);
            }
        }

        // Now recalculate all the edges for this hexagon
        h.RecalculateEdges(neighbours, transform.localToWorldMatrix);
    }





    private TerrainChunk FindOrCreateChunk(Vector2Int chunk)
    {
        // Chunk does not already exist
        if (!Chunks.ContainsKey(chunk))
        {
            // Create it
            AddChunk(chunk);
        }

        Chunks.TryGetValue(chunk, out TerrainChunk c);
        return c;
    }



    public void AddHexagon(Vector3Int worldCellPosition, float heightMultiplier)
    {
        // Calculate which Chunk the Hexagon should be in
        Vector2Int chunk = CellToChunk(worldCellPosition);
        TerrainChunk c = FindOrCreateChunk(chunk);

        if (c != null)
        {
            // Now round the height 
            float roundedMultiplier = heightMultiplier;
            // Only round it if we are making terraces
            if (MakeTerraces)
            {
                roundedMultiplier = (float)Mathf.RoundToInt(heightMultiplier * MaxTerraces) / MaxTerraces;
            }

            // Evaluate the point on the height map to get a good distribution of heights
            roundedMultiplier = HeightCurve.Evaluate(roundedMultiplier);

            // Calculate the height
            Vector3 worldHeight = Grid.GetCellCenterWorld(worldCellPosition) - new Vector3(0, 0, roundedMultiplier * HeightOffset);

            // Add the Hex to that chunk
            c.AddHex(worldCellPosition, roundedMultiplier, worldHeight);
        }
        else
        {
            Debug.LogError("Could not find the correct Chunk to add the Hexagon to.");
        }

    }


    public void AddHexagons(Vector3Int[] worldCellPositions, float[] heightMultipliers)
    {
        // Ensure the arrays are the same size
        if (worldCellPositions.Length == heightMultipliers.Length)
        {
            for (int i = 0; i < worldCellPositions.Length; i++)
            {
                AddHexagon(worldCellPositions[i], heightMultipliers[i]);
            }
        }
        else
        {
            Debug.LogError("Arrays for Hex cell positions and their respective heights must be the same size.");
        }
    }




    public Hex FindHex(Vector3Int cell)
    {
        // Get the valid chunk
        TerrainChunk c = FindOrCreateChunk(CellToChunk(cell));
        if (c != null)
        {
            if (c.Hexagons.TryGetValue(cell, out Hex h))
            {
                return h;
            }
        }
        return null;
    }



    private void AddChunk(Vector2Int chunkPosition)
    {
        // Chunk does not already exist
        if (!Chunks.ContainsKey(chunkPosition))
        {
            // Instantiate a new chunk and add it
            TerrainChunk c = InstantiateTerrainChunk(chunkPosition);
            Chunks.Add(chunkPosition, c);
        }
    }



    private TerrainChunk InstantiateTerrainChunk(Vector2Int chunkPosition)
    {
        // Create the new GameObject
        GameObject g = new GameObject("Chunk " + chunkPosition.ToString());
        g.transform.parent = AllChunksParent;

        TerrainChunk c = g.AddComponent<TerrainChunk>();
        c.CreateTerrainChunk(chunkPosition, Grid, GroundMaterial);

        return c;
    }



    private static void OptimiseMesh(Mesh m)
    {
        // Apply all optimisations
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        m.Optimize();
    }





    public Vector3 CellToWorldPosition(Vector3Int cell)
    {
        return Grid.GetCellCenterWorld(cell);
    }


    private Vector2Int CellToChunk(Vector3Int cell)
    {
        return new Vector2Int(cell.x / ChunkSizeInHexagons, cell.y / ChunkSizeInHexagons);
    }



    public static Vector3Int FirstHexagonCellInChunk(Vector2Int chunk)
    {
        return new Vector3Int(chunk.x * ChunkSizeInHexagons, chunk.y * ChunkSizeInHexagons, 0);
    }

    public static Vector3Int LastHexagonCellInChunk(Vector2Int chunk)
    {
        return new Vector3Int(chunk.x * ChunkSizeInHexagons + ChunkSizeInHexagons - 1, chunk.y * ChunkSizeInHexagons + ChunkSizeInHexagons - 1, 0);
    }




    /// <summary>
    /// A class for storing one Chunk's meshes and Hexagons.
    /// </summary>
    public class TerrainChunk : MonoBehaviour
    {
        public Vector2Int Chunk { get; private set; }
        private Grid grid;
        private Material GroundMaterial;

        public Dictionary<Vector3Int, Hex> Hexagons { get; private set; } = new Dictionary<Vector3Int, Hex>();

        public Transform MeshParent;
        public MeshFilter[] AllMeshes => MeshParent.GetComponentsInChildren<MeshFilter>();


        public MeshCollider Collider;

        public void CreateTerrainChunk(Vector2Int chunk, Grid grid, Material groundMaterial)
        {
            Chunk = chunk;
            this.grid = grid;
            GroundMaterial = groundMaterial;
            MeshParent = transform;

            //Collider = gameObject.AddComponent<MeshCollider>();
        }

        public void AddHex(Vector3Int cell, float heightMultiplier, Vector3 faceCentreWorldPosition)
        {
            if (!Hexagons.ContainsKey(cell))
            {
                // Create the new hexagon
                Hex h = new Hex(cell, transform.localToWorldMatrix, faceCentreWorldPosition, heightMultiplier, Hex.Terrain.Land);

                Hexagons.Add(cell, h);
            }
            else
            {
                Debug.LogError("Can't add Hex cell " + cell.ToString() + " in Chunk " + Chunk.ToString() + " as it already exists.");
            }
        }


        /// <summary>
        /// Merges all sub meshes and returns the number of meshes created.
        /// </summary>
        /// <returns></returns>
        public int MergeAllMeshes(bool makeTerraces)
        {
            // Get all the meshes
            Dictionary<float, List<CombineInstance>> heightToMesh = new Dictionary<float, List<CombineInstance>>();

            // Sort the meshes by height
            foreach (Hex h in Hexagons.Values)
            {
                // Check that the entry exists
                if (!heightToMesh.TryGetValue(h.HeightMultiplier, out List<CombineInstance> meshes))
                {
                    // Create a new list and add it as a value
                    meshes = new List<CombineInstance>();
                    heightToMesh[h.HeightMultiplier] = meshes;
                }

                // Add the face of the hexagon
                meshes.Add(h.FaceCombineInstance);

                // Add any other meshes now

                if (h.Edges != null)
                {
                    // Add all the hexagons edges
                    foreach (CombineInstance c in h.Edges)
                    {
                        meshes.Add(c);
                    }
                }
            }

            List<MeshFilter> allSubMeshes = new List<MeshFilter>();


            // Here make a seperate mesh for each height layer
            if (makeTerraces)
            {
                // Loop through all meshes for each height
                foreach (float key in heightToMesh.Keys)
                {
                    heightToMesh.TryGetValue(key, out List<CombineInstance> meshes);

                    // Combine all the meshes
                    Mesh m = new Mesh();
                    m.CombineMeshes(meshes.ToArray(), true);

                    // Optimise the mesh
                    OptimiseMesh(m);

                    // Create the new mesh filter object
                    allSubMeshes.Add(InstantiateMeshFilter(m, key, GroundMaterial));
                }
            }
            // Just use one mesh
            else
            {
                // Combine all the meshes here
                List<CombineInstance> allMeshes = new List<CombineInstance>();
                foreach (float key in heightToMesh.Keys)
                {
                    if (heightToMesh.TryGetValue(key, out List<CombineInstance> meshes))
                    {
                        allMeshes.AddRange(meshes);
                    }
                }


                // Combine all the meshes
                Mesh m = new Mesh();
                m.CombineMeshes(allMeshes.ToArray(), true);

                // Optimise the mesh
                OptimiseMesh(m);

                // Create the new mesh filter object
                allSubMeshes.Add(InstantiateMeshFilter(m, -1, GroundMaterial));
            }


            return allSubMeshes.Count;
        }



        private MeshFilter InstantiateMeshFilter(Mesh m, float heightMultiplier, Material material)
        {
            // Create the new GameObject
            GameObject g = new GameObject("Mesh Layer " + heightMultiplier.ToString());
            g.transform.parent = MeshParent;

            MeshFilter f = g.AddComponent<MeshFilter>();
            f.mesh = m;

            MeshRenderer r = g.AddComponent<MeshRenderer>();

            r.sharedMaterial = material;
            r.material.SetFloat("Height", heightMultiplier);



            return f;
        }


        public void OptimiseAllMeshes()
        {
            // Loop through each mesh and optimise it
            foreach (MeshFilter f in AllMeshes)
            {
                Mesh m = f.mesh;
                OptimiseMesh(m);
                f.mesh = m;
            }
        }



        public void ClearAll()
        {
            Hexagons.Clear();
            ClearMesh();
        }


        public void ClearMesh()
        {
            for (int i = 0; i < MeshParent.childCount; i++)
            {
                // Destroy all the game objects
                Destroy(MeshParent.GetChild(i).gameObject);
            }
        }

    }


    public class Hex
    {
        public readonly Vector3Int Cell;
        public readonly float HeightMultiplier;
        public readonly Vector3 CentreOfFaceWorld;

        public Mesh Face;
        private readonly Matrix4x4 transform;
        public CombineInstance FaceCombineInstance { get { return new CombineInstance() { mesh = Face, transform = transform, }; } }

        public List<CombineInstance> Edges = new List<CombineInstance>();

        public Terrain TerrainType;

        public Hex(Vector3Int cell, Matrix4x4 transform, Vector3 centreOfFace, float heightMultiplier, Terrain terrainType)
        {
            Cell = cell;
            HeightMultiplier = heightMultiplier;

            this.transform = transform;
            TerrainType = terrainType;
            CentreOfFaceWorld = centreOfFace;

            Face = GenerateFaceMesh(centreOfFace);
        }


        private Mesh GenerateFaceMesh(Vector3 centreOfFace)
        {
            // Don't use this as our hexagons aren't EXACTLY mathematically perfect, but good enough for the grid
            /*
            float sqrt3 = Mathf.Sqrt(3);
            float xOffset = (sqrt3 * HexagonEdgeLength) / 2;
            */

            // Get the positions of the vertices of the face of the hex
            Vector3 top = centreOfFace + new Vector3(0, HexagonEdgeLength);
            Vector3 bottom = centreOfFace + new Vector3(0, -HexagonEdgeLength);

            Vector3 topLeft = centreOfFace + new Vector3(-HexagonEdgeLength, HexagonEdgeLength / 2);
            Vector3 topRight = centreOfFace + new Vector3(HexagonEdgeLength, HexagonEdgeLength / 2); ;

            Vector3 bottomLeft = centreOfFace + new Vector3(-HexagonEdgeLength, -HexagonEdgeLength / 2);
            Vector3 bottomRight = centreOfFace + new Vector3(HexagonEdgeLength, -HexagonEdgeLength / 2);

            // Create the mesh
            Mesh m = new Mesh
            {
                // Add vertices
                vertices = new Vector3[] { top, topRight, bottomRight, bottom, bottomLeft, topLeft, centreOfFace },
                // Add the triangles of the mesh
                triangles = new int[]
                {
                    6, 0, 1,
                    6, 1, 2,
                    6, 2, 3,
                    6, 3, 4,
                    6, 4, 5,
                    6, 5, 0,
                },
                // Add the normals (this is the face so normal is up)
                normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up },
            };

            return m;
        }

        public void RecalculateEdges(List<Hex> neighbours, Matrix4x4 transform)
        {
            // Re-assign the edges variable
            Edges = GenerateEdgeMeshesForNeighbours(neighbours, transform);
        }

        private List<CombineInstance> GenerateEdgeMeshesForNeighbours(List<Hex> neighbours, Matrix4x4 transform)
        {
            List<CombineInstance> newMeshesToAdd = new List<CombineInstance>();

            // Check each neighbour
            foreach (Hex neighbour in neighbours)
            {
                // Don't check it's self and ensure we actually want to create edges here
                if (!neighbour.Cell.Equals(Cell) && HeightMultiplier > neighbour.HeightMultiplier)
                {
                    List<(Vector3, Vector3)> sharedVertices = new List<(Vector3, Vector3)>();

                    // Loop through all possibilities
                    foreach (Vector3 neighbourVertex in neighbour.Face.vertices)
                    {
                        foreach (Vector3 vertex in Face.vertices)
                        {
                            // Hexagons are next to each other and current is above the neighbour
                            if (neighbourVertex.x == vertex.x && neighbourVertex.y == vertex.y && HeightMultiplier > neighbour.HeightMultiplier)
                            {
                                sharedVertices.Add((vertex, neighbourVertex));
                                // Break out to save a litte time
                                if (sharedVertices.Count == 2)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // Should have 2 shared vertex pairs if hexagons are neighbours and on different heights
                    if (sharedVertices.Count == 2)
                    {
                        // Make sure to assign the triangles in clockwise order
                        sharedVertices.Sort((x, y) => Clockwise.Compare(x.Item1, y.Item1, CentreOfFaceWorld));
                        Vector3 a = sharedVertices[0].Item1, b = sharedVertices[0].Item2, c = sharedVertices[1].Item1, d = sharedVertices[1].Item2;

                        // Calculate the normal for that face
                        Vector3 midpointOfEdge = a + ((c - a) / 2);
                        Vector3 normal = Vector3.Normalize(midpointOfEdge - CentreOfFaceWorld);

                        // Once we have all shared vertices, construct the mesh
                        Mesh m = new Mesh
                        {
                            vertices = new Vector3[] { a, b, c, d },
                            triangles = new int[]
                            {
                                0, 1, 3,
                                0, 3, 2
                            },
                            normals = new Vector3[] { normal, normal, normal, normal },
                        };

                        // Add the mesh to the list of meshes to be merged
                        CombineInstance com = new CombineInstance
                        {
                            mesh = m,
                            transform = transform
                        };

                        newMeshesToAdd.Add(com);
                    }
                }
            }

            // Once we get here all neighbours have been checked
            return newMeshesToAdd;
        }


        public enum Terrain
        {
            Land,
            Water,
        }


        public static List<Vector3Int> CalculatePossibleNeighbourCells(in Vector3Int current)
        {
            // Convoluted way of calculating the neighbour positions of a pointy hex grid cell
            Vector3Int upLeft = current + new Vector3Int(-1, -1, 0);
            Vector3Int upRight = current + new Vector3Int(0, -1, 0);

            Vector3Int downLeft = current + new Vector3Int(-1, 1, 0);
            Vector3Int downRight = current + new Vector3Int(0, 1, 0);

            Vector3Int left = current + new Vector3Int(-1, 0, 0);
            Vector3Int right = current + new Vector3Int(1, 0, 0);

            // Y is an odd number so need to move to right instead of left
            if (current.y % 2 == 1)
            {
                upLeft = current + new Vector3Int(1, -1, 0);
                downLeft = current + new Vector3Int(1, 1, 0);
            }

            return new List<Vector3Int>(new Vector3Int[] { upLeft, upRight, right, downRight, downLeft, left });
        }
    }
}

public static class Clockwise
{
    public static int Compare(Vector2 first, Vector2 second, Vector2 centre)
    {
        Vector2 firstOffset = first - centre;
        Vector2 secondOffset = second - centre;

        // Get the angles in degrees
        float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y) * Mathf.Rad2Deg;
        float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y) * Mathf.Rad2Deg;

        // Ensure we always have positive angles (go clockwise)
        while (angle1 < 0) angle1 += 360;
        while (angle2 < 0) angle2 += 360;


        // For some reason it my gen code does not like it when the angle is 0. 
        // Janky fix for now
        if (angle1 == 0)
        {
            float temp = angle1;
            angle1 = angle2;
            angle2 = temp;
        }

        // Compare them
        return angle1.CompareTo(angle2);
    }


}
