using UnityEngine;

public static class GroundCheck
{
    public const string GROUND_LAYER = "Ground";
    public static LayerMask GroundMask => LayerMask.GetMask(GROUND_LAYER);

    public const float DEFAULT_RAYCAST_DISTANCE = 10f;

    public static LayerMask SolidLayers => LayerMask.GetMask(GROUND_LAYER, "Water", "WorldObject");


    public static Collider[] DoSphereCast(Vector3 worldPosition, float collisionCheckRadius)
    {
        return Physics.OverlapSphere(worldPosition, collisionCheckRadius, GroundMask);
    }

    public static bool DoRaycastDown(Vector3 worldPosition, out RaycastHit hit, float maxRaycastDistance = DEFAULT_RAYCAST_DISTANCE)
    {
        return DoRaycast(worldPosition, Vector3.down, out hit, maxRaycastDistance);
    }

    public static bool DoRaycast(Vector3 worldPosition, Vector3 direction, out RaycastHit hit, float maxRaycastDistance = DEFAULT_RAYCAST_DISTANCE)
    {
        //Debug.DrawLine(worldPosition, worldPosition + (direction * maxRaycastDistance), Color.white, 1000);
        return Physics.Raycast(worldPosition, direction, out hit, maxRaycastDistance, SolidLayers);
    }
}
