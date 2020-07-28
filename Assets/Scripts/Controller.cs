using UnityEngine;

public static class Controller
{
    public const float TouchMultiplier = 1;


    public static Vector2 GetDeltaPositionScaled()
    {
        // Use touchscreen
        if (Input.touchSupported)
        {
            if (Input.touches.Length > 0)
            {
                return Input.touches[0].deltaPosition * Input.touches[0].deltaTime;
            }
        }
        // Use mouse
        else if (Input.mousePresent)
        {
            //Debug.Log("Touch not supported, use mouse scroll");
            Vector2 scrollDelta = Input.mouseScrollDelta;

            return new Vector2(scrollDelta.y, scrollDelta.y);
        }

        return Vector2.zero;
    }

}
