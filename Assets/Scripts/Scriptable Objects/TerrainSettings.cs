using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    [Header("Settings")]
    public bool UseCurve = false;
    public AnimationCurve HeightDistribution;
    public float HeightMultiplier = 16;

    [Space]
    public Biome MainBiome;

    [Header("Bunkers")]
    public bool DoBunkers = true;
    public float BunkerMultiplier = 4f;
    public Vector2 BunkerNoiseThresholdMinMax = new Vector2(0.75f, 1.5f);

    [Header("Holes")]
    public Vector2 HoleNoiseThresholdMinMax = new Vector2(0.6f, 1.5f);

    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;
    public int TerrainDivisions => SamplePointFrequency - 1;


    public override void ValidateValues()
    {
    }


    public enum Biome
    {
        Grass,
        Sand,
        Hole,
        Water,
        Ice,
    }
}
