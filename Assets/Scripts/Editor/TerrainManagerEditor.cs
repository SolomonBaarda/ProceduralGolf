using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainManager m = (TerrainManager)target;

        if (GUILayout.Button("Skip to end of course"))
        {
            var current = m.CurrentLoadedTerrain.Courses[m.GolfBall.Progress.CurrentCourse];
            m.GolfBall.transform.position = current.Hole;
        }

    }


}
