using UnityEngine;

public static class Controller
{
    public const float TouchMultiplier = 10;


    public static bool UsingTouch => Input.touchSupported;




    public static Vector2 DeltaPosition(RectTransform screenArea)
    {
        Vector2 currentMax = Vector2.zero;

        foreach (Touch t in Input.touches)
        {
            // Ensure the touch is within the area
            if (RectTransformUtility.RectangleContainsScreenPoint(screenArea, t.position))
            {
                // Update the value if it is the new largest
                Vector2 possibleValue = t.deltaPosition;
                currentMax = possibleValue.magnitude > currentMax.magnitude ? possibleValue : currentMax;
            }
        }

        return currentMax;
    }




}
