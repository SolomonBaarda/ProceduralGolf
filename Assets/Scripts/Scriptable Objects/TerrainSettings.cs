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

    [Header("Procedural Objects")]
    [Range(1, 10)]
    public int PoissonSamplingIterations = 5;
    public float PoissonSamplingRadius = 1;
    public List<ProceduralObject> ProceduralObjects;

    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;


    public override void ValidateValues()
    {
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

    //public float Radius = 20;

    

    [Serializable]
    public class ProceduralObject
    {
        public bool Do = true;
        [Min(0.1f)]
        public List<Biome.Type> RequiredBiomes;
        public List<GameObject> Prefabs;

        public bool UseMask = false;
        [Header("List of layer indexes to use as mask")]
        public List<Mask> Masks;
    }
}
