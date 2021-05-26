using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private GameManager G => (GameManager) target;


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (G.TerrainMode == GameManager.TerrainGenerationMethod.Testing)
        {
            if (GUILayout.Button("Generate again"))
            {
                G.GenerateAgain();
            }
        }

        /*
        if (GUILayout.Button("Clear terrain"))
        {
            G.Clear();
        }
        */

        /*
        if (GUILayout.Button("Update world save references"))
        {
            UpdateAllWorldSaveReferences();
        }
        */
    }


    public void UpdateAllWorldSaveReferences()
    {
        foreach (TerrainData d in G.WorldSaves)
        {
            TerrainData data = d;

            TerrainSaver.UpdateReferences(ref data);
        }
    }



}
