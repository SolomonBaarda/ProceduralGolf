using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    public bool UseCurve = false;
    public AnimationCurve HeightDistribution;
    public float HeightMultiplier = 16;

    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;
    public int TerrainDivisions => SamplePointFrequency - 1;


    public override void ValidateValues()
    {
    }

}
