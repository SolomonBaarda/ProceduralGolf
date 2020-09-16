using System;
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



        if (m.CurrentLoadedTerrain != null)
        {
            if (GUILayout.Button("Save terrain to file"))
            {
                // Save the currently used TerrainData to file
                TerrainSaver.SaveTerrain(m.CurrentLoadedTerrain);
            }
        }
    }






}
