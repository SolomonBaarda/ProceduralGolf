using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GolfBall))]
public class GolfBallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GolfBall g = (GolfBall)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Shoot"))
        {
            g.Shoot();
        }
    }
}


