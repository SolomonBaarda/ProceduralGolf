using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(MyTerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MyTerrainGenerator t = (MyTerrainGenerator)target;
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
