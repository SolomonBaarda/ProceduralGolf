using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
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
            g.UpdateAllWorldSaveReferences();
        }

    }






}
