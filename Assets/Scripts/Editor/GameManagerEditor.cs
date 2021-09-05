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
    }
}
