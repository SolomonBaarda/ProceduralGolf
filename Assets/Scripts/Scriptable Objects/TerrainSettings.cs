using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    [Header("Settings")]
    public bool UseCurve = false;
    public AnimationCurve HeightDistribution;
    public float HeightMultiplier = 16;

    [Space]
    public Biome.Type MainBiome;

    [Header("Holes")]
    public Vector2 HoleNoiseThresholdMinMax = new Vector2(0.8f, 1.5f);
    public Biome.Type HoleBiome;

    [Header("Terrain Cutouts")]
    public Cutout Bunker;
    public Cutout Lake;

    [Header("Procedural objects")]
    public ProceduralObject Trees;
    public ProceduralObject Rocks;



    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;
    public int TerrainDivisions => SamplePointFrequency - 1;


    public override void ValidateValues()
    {
    }



    [Serializable]
    public class ProceduralObject
    {
        public bool DoObject = true;
        public float SamplePointRadius = 1f;
        public Vector2 NoiseThresholdMinMax = new Vector2(0.75f, 1.5f);

        public Biome.Type DesiredBiome;
    }


    [Serializable]
    public class Cutout
    {
        public bool Do = true;
        public float Multiplier = 1;
        public Vector2 NoiseThresholdMinMax = new Vector2(0.75f, 1.5f);

        public Biome.Type Biome;
    }
}
