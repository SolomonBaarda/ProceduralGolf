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

        if (GUILayout.Button("Reset"))
        {
            g.rigid.velocity = Vector3.zero;
            g.transform.position = new Vector3(0, 50, 0);
        }
    }
}


