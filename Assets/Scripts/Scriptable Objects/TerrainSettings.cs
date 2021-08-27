using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    [Header("Main Settings")]
    public bool UseCurve = false;
    public AnimationCurve HeightDistribution;
    public float HeightMultiplier = 1;
    public Biome.Type MainBiome;

    [Header("Terrain Layers")]
    public List<Layer> TerrainLayers = new List<Layer>();

    [Header("Procedural Objects")]
    [Range(1, 10)]
    public int PoissonSamplingIterations = 5;
    public float PoissonSamplingRadius = 1;
    public List<ProceduralObject> ProceduralObjects;

    [Header("Greens")]
    public List<Green> Greens;

    [Header("Holes")]
    public float MinDistanceBetweenHoles = 100;
    public List<Biome.Type> ValidHoleBiomes;

    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;

    public readonly int GreenMinVertexCount = 100;



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
        [Space]
        public bool UseMask = false;
        public bool UseDistanceFromOriginMultiplier = false;
        public float DistanceFromOriginRadius = 0;
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

    [Serializable]
    public class ProceduralObject
    {
        public bool Do = true;
        public List<Biome.Type> RequiredBiomes;
        public List<GameObject> Prefabs;
        [Range(0, 1)]
        public float Chance = 1;
        [Space]
        public bool UseMask = false;
        [Header("List of layer indexes to use as mask")]
        public List<Mask> Masks;
    }

    [Serializable]
    public class Green
    {
        public bool Do = true;
        public List<Biome.Type> RequiredBiomes;
        [Space]
        public bool UseMask = false;
        [Header("List of layer indexes to use as mask")]
        public List<Mask> Masks;
    }

}
