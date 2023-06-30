using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Mesh")]
public class MeshSettings : VariablePreset
{
    [Header("Value of 0 (most detail) to 6 (least detail)")]
    public int LevelOfDetail = 1;

    public int SimplificationIncrement => Mathf.Max(LevelOfDetail * 2, 1);

    public override void ValidateValues()
    {
        LevelOfDetail = Mathf.Clamp(LevelOfDetail, 0, 6);
    }


}
