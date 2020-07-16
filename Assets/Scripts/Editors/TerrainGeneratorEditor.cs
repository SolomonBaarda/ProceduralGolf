using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator t = (TerrainGenerator)target;
        DrawDefaultInspector();

        if (GUILayout.Button("Clear"))
        {
            t.Clear();
        }

        if (GUILayout.Button("Generate"))
        {
            t.Generate();
        }
    }

}
