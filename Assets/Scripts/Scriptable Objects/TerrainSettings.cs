using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    [Header("Main Settings")]
    public int NumChunksToGenerateSize = 5;
    public bool UseCurve = false;
    public AnimationCurve HeightDistribution;
    public float HeightMultiplier = 1;
    public Biome.Type MainBiome;
    public bool ForceMinHeightZero = true;

    [Header("Terrain Layers")]
    public List<LayerSettings> TerrainLayers = new List<LayerSettings>();

    [Header("Procedural Objects")]
    [Range(1, 10)]
    public int PoissonSamplingIterations = 5;
    public float PoissonSamplingRadius = 1;
    public List<ObjectSettings> ProceduralObjects;

    [Header("Course")]
    public List<CourseSettings> Course;

    [Header("Holes")]
    public float MinDistanceBetweenHoles = 100;
    public int AreaToCheckValidHoleBiome = 2;
    public List<Biome.Type> ValidHoleBiomes;

    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 241;

    public readonly int GreenMinVertexCount = 5000;



    public override void ValidateValues()
    {
    }


    [Serializable]
    public class LayerSettings
    {
        [Header("Conditions")]
        public bool Apply = true;
        public NoiseSettings Settings;
        public bool UseDistanceFromOriginCurve = false;
        public AnimationCurve DistanceFromOriginCurve;
        public float NoiseThresholdMin = 0.0f;
        public float NoiseThresholdMax = 1.0f;

        [Header("Output")]
        public float Multiplier = 1.0f;
        public Biome.Type Biome;
        public bool ClampHeightToZero = true;
        public Mode CombinationMode;
        public enum Mode { Add, Subtract, Divide, Multiply, Modulus, Set };

        [Header("Share another layer's noise")]
        public bool ShareOtherLayerNoise = false;
        public int LayerIndexShareNoise = 0;

        [Header("Use layer as mask")]
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

    [Serializable]
    public class ObjectSettings
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
    public class CourseSettings
    {
        public bool Do = true;
        public List<Biome.Type> RequiredBiomes;
        [Space]
        public bool UseMask = false;
        [Header("List of layer indexes to use as mask")]
        public List<Mask> Masks;
    }

}
