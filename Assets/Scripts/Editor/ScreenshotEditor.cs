using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Screenshot))]
public class ScreenshotEditor : Editor
{


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Screenshot s = (Screenshot)target;

        if (GUILayout.Button("Take current resolution screenshot"))
        {
            Screenshot.TakeBasicScreenshot();
        }
        if (GUILayout.Button("Take full resolution screenshot (no UI)"))
        {
            s.TakeFullResolutionScreenshot();
        }

    }


}
