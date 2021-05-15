using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    [Header("Settings")]
    public bool UseCurve = false;
    public AnimationCurve HeightDistribution;

    [Space]
    public Biome.Type MainBiome;
    public float HeightMultiplier = 1;

    [Header("Terrain Layers")]
    public List<Layer> TerrainLayers = new List<Layer>();

    [Header("Procedural objects")]
    public ProceduralObject Trees;
    public ProceduralObject Rocks;

    [Header("Noise settings")]
    public NoiseSettings NoiseTree;
    public NoiseSettings NoiseRock;



    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;


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
    public class Layer
    {
        public bool Apply = true;
        public float Multiplier = 1;
        public Biome.Type Biome;
        public float NoiseThresholdMin = 0;
        public float NoiseThresholdMax = 1;

        public Mode CombinationMode;
        public enum Mode { Add, Subtract, Divide, Multiply, Modulus, Set };

        public NoiseSettings Settings;

        public bool UseMask = false;
        [Header("List of layer indexes to use as mask")]
        public List<Mask> Masks;
    }

    [Serializable]
    public class Mask
    {
        public int LayerIndex;
        public float NoiseThresholdMin = 0;
        public float NoiseThresholdMax = 1;
    }
}
