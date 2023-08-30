using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private GameManager G => (GameManager)target;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            G.StartGeneration(G.TerrainGenerator.CurrentSettings, true, null);
        }
    }
}
