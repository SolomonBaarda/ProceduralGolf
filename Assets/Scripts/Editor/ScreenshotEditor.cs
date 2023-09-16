using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Screenshot))]
public class ScreenshotEditor : Editor
{


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Screenshot s = (Screenshot)target;

        if (GUILayout.Button("4K"))
        {
            s.Width = 3840;
            s.Height = 2160;
        }
        if (GUILayout.Button("ICON"))
        {
            s.Width = 2048;
            s.Height = 2048;
        }
        if (GUILayout.Button("BANNER"))
        {
            s.Width = 4096;
            s.Height = 2000;
        }

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
