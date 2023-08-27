using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Terrain")]
public class TerrainSettings : VariablePreset
{
    [Header("Main Settings")]
    [Min(1)]
    public int NumChunksToGenerateSize = 10;

    public float CourseCameraPathSimplificationStrength = 10.0f;

    public float BaseHeight = 1.0f;
    public float HeightMultiplier = 1.0f;
    public Biome.Type BackgroundBiome;
    public bool ForceMinHeightZero = true;

    [Header("Water Settings")]
    public bool DoWater = false;
    public float WaterHeight = 0.0f;
    public Biome.Type UnderwaterBiome;

    [Header("Terrain Layer Settings")]
    public List<LayerSettings> TerrainLayers = new List<LayerSettings>();

    [Header("Procedural Object Settings")]
    [Range(1, 10)]
    public int PoissonSamplingIterations = 5;
    public float PoissonSamplingRadius = 1;
    public List<ObjectSettings> ProceduralObjects;

    [Header("Course Settings")]
    public List<CourseSettings> Course;

    [Header("Hole Settings")]
    public List<CourseSettings> Holes;

    public bool FlattenStartAndHole = false;
    public int MinDistanceRadiusToFlatten = 4;
    public int MaxDistanceRadiusToFlatten = 6;
    public float AbsoluteHeightToRaiseFlattenedArea = 2.0f;

    [Space]
    [Min(1)]
    public float MinimumWorldDistanceBetweenHoles = 250.0f;

    /// <summary>
    /// Number of Noise sample points taken in each chunk.
    /// </summary>
    public readonly int SamplePointFrequency = 61;

    /// <summary>
    /// The minimum number of start/end positions allowed for a course. Use this value to exclue smaller courses from being valid
    /// </summary>
    public readonly int FairwayMinStartEndVertexCount = 50;



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
        public bool OneMinus = false;
        public float Offset = 0.0f;
        public float Multiplier = 1.0f;
        public Biome.Type Biome;
        public bool ClampHeightToZero = true;
        public Mode CombinationMode;
        public enum Mode { Add, Subtract, Divide, Multiply, Modulus, Set, Pow };

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
