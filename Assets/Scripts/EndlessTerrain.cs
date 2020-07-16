using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float MaxViewDistance = 300f;
    public Transform Viewer;

    public static Vector2 ViewerPosition;

    private const int chunkSize = HexMap.ChunkSizeInHexagons;
    private int chunksVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / chunkSize);





}
