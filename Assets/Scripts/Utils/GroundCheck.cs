using UnityEngine;

public static class GroundCheck
{
    public const float DEFAULT_RAYCAST_DISTANCE = 10f;

    public static LayerMask GroundMask => LayerMask.GetMask("Ground");

    public static LayerMask SolidObjectsMask => LayerMask.GetMask("Ground", "Water", "WorldObject", "Water");
}
