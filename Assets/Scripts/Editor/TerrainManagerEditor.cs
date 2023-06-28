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

    }






}
