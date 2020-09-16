using System;
using UnityEditor;
using UnityEngine;


[ExecuteInEditMode]
[CustomEditor(typeof(CourseManager))]
public class CourseManagerEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CourseManager c = (CourseManager)target;



        if (c.HolesHaveBeenOrdered && c.NumberOfHoles > 1)
        {
            if (GUILayout.Button("Teleport to hole"))
            {
                c.TeleportBallToNextHole();
            }
        }
    }






}
