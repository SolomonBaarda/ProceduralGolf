using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Mesh")]
public class MeshSettings : VariablePreset
{
    [Header("Value of 0 (most detail) to 6 (least detail)")]
    public List<int> LevelOfDetail = new List<int>() { 0 };

    public override void ValidateValues()
    {
        for (int i = 0; i < LevelOfDetail.Count; i++)
        {
            LevelOfDetail[i] = System.Math.Max(LevelOfDetail[i], 0);
        }
    }


}
