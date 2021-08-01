using UnityEngine;

public static class GroundCheck
{
    public const string GROUND_LAYER = "Ground";
    public static int GroundLayer => LayerMask.NameToLayer(GROUND_LAYER);
    public static int GroundMask => LayerMask.GetMask(GROUND_LAYER);

    public const float DEFAULT_RADIUS = 0.2f;

    public const float DEFAULT_RAYCAST_DISTANCE = 10f;


    public static Collider[] GetGroundCollisions(Vector3 worldPosition, float collisionCheckRadius = DEFAULT_RADIUS)
    {
        // Get all collisions
        return Physics.OverlapSphere(worldPosition, collisionCheckRadius, GroundMask);
    }

    public static bool IsOnGround(Vector3 worldPosition, float collisionCheckRadius = DEFAULT_RADIUS)
    {
        return GetGroundCollisions(worldPosition, collisionCheckRadius).Length != 0;
    }

    public static bool DoRaycastDown(Vector3 worldPosition, out RaycastHit hit, float maxRaycastDistance = DEFAULT_RAYCAST_DISTANCE)
    {
        //Debug.DrawLine(worldPosition, worldPosition + -TerrainManager.UP * maxRaycastDistance, Color.white, 10);
        return Physics.Raycast(worldPosition, -TerrainManager.UP, out hit, maxRaycastDistance, GroundMask);
    }
}
