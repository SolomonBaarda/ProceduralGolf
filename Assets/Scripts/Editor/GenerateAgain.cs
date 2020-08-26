using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(GameManager))]
public class GenerateAgain : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager g = (GameManager)target;


        if (g.TerrainMode == GameManager.TerrainGenerationMethod.FixedArea)
        {
            if (GUILayout.Button("Generate again"))
            {
                g.GenerateAgain();
            }
        }

        if (GUILayout.Button("Clear terrain"))
        {
            g.Clear();
        }

        if (GUILayout.Button("Update world save references"))
        {
            foreach(TerrainData d in g.WorldSaves)
            {
                TerrainData data = d;
                TerrainSaver.UpdateReferences(ref data);
            }
        }




    }






}
