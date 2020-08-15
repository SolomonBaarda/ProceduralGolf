using UnityEngine;


public class TerrainChunkGizmos : MonoBehaviour
{
    public TerrainMap TerrainMap;

    [Min(0)]
    public float Length = 2;
    public bool UseLOD = true;
    private int LODIncrement = 1;


    public void SetMap(TerrainMap map)
    {
        TerrainMap = map;
    }

    public void SetLOD(MeshSettings settings)
    {
        LODIncrement = settings.SimplificationIncrement;
    }


    private void OnDrawGizmosSelected()
    {
        if (TerrainMap != null)
        {
            int i = UseLOD ? LODIncrement : 1;

            for (int y = 0; y < TerrainMap.Map.GetLength(1); y += i)
            {
                for (int x = 0; x < TerrainMap.Map.GetLength(0); x += i)
                {
                    TerrainMap.Point p = TerrainMap.Map[x, y];

                    Color c = Color.black;
                    switch (p.Biome)
                    {
                        case TerrainSettings.Biome.Grass:
                            c = Color.green;
                            break;
                        case TerrainSettings.Biome.Sand:
                            c = Color.yellow;
                            break;
                        case TerrainSettings.Biome.Hole:
                            c = Color.gray;
                            break;
                        case TerrainSettings.Biome.Water:
                            c = Color.blue;
                            break;
                        case TerrainSettings.Biome.Ice:
                            c = Color.white;
                            break;
                    }

                    Gizmos.color = c;
                    Vector3 pos = p.LocalVertexPosition + TerrainMap.Bounds.center;
                    Gizmos.DrawLine(pos, pos + (TerrainGenerator.UP * Length));
                }
            }
        }
    }
}
