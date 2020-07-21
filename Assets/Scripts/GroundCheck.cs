using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public const string GROUND_LAYER = "Ground";
    public static int GroundLayer => LayerMask.NameToLayer(GROUND_LAYER);
    public static int GroundMask => LayerMask.GetMask(GROUND_LAYER);

    public const float DEFAULT_RADIUS = 0.2f;


    public static Collider[] GetGroundCollisions(Vector3 worldPosition, float collisionCheckRadius = DEFAULT_RADIUS)
    {
        // Get all collisions
        return Physics.OverlapSphere(worldPosition, collisionCheckRadius, GroundMask);
    }

    public static bool IsOnGround(Vector3 worldPosition, float collisionCheckRadius = DEFAULT_RADIUS)
    {
        return GetGroundCollisions(worldPosition, collisionCheckRadius).Length != 0;
    }

}
