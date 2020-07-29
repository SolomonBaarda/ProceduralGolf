using UnityEngine;

public class CourseManager : MonoBehaviour
{
    public GolfBall GolfBall;


    public TerrainChunkManager AllTerrain;


    public void RespawnGolfBall()
    {
        RespawnGolfBall(CalculateSpawnPoint(GolfBall.Radius));
    }

    public void RespawnGolfBall(Vector3 position)
    {
        // Reset
        GolfBall.StopAllCoroutines();
        GolfBall.Reset();
        GolfBall.GameStats.Reset();

        // Position
        GolfBall.transform.position = position;
        // Freeze the ball
        GolfBall.WaitForNextShot();
    }


    public Vector3 CalculateSpawnPoint(float sphereRadius)
    {
        TerrainChunk chunk = AllTerrain.GetChunk(Vector2Int.zero);
        Vector3 centre3 = chunk.Bounds.center;
        Vector2 centre = new Vector2(centre3.x, centre3.z);

        Vector3 closest = chunk.Collider.vertices[0];
        int index = 0;

        for (int i = 0; i < chunk.Collider.vertices.Length; i++)
        {
            // Update the closest point
            if (Vector2.Distance(centre, new Vector2(chunk.Collider.vertices[i].x, chunk.Collider.vertices[i].z)) < Vector3.Distance(centre, new Vector3(closest.x, closest.z)))
            {
                closest = chunk.Collider.vertices[i];
                index = i;
            }
        }


        Vector3 normal = chunk.Collider.normals[index];
        // Move the point in the direction of the normal a little
        closest += normal.normalized * sphereRadius;

        return closest;
    }





}
