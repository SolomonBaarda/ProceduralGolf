using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Noise")]
public class NoiseSettings : VariablePreset
{
    [Header("General")]
    public FastNoiseLite.RotationType3D RotationType;
    public float Frequency = 0.05f;
    public FastNoiseLite.NoiseType NoiseType;
    public Vector2 Offset = Vector2.zero;

    [Header("Fractal")]
    public FastNoiseLite.FractalType FractalType;
    public int Octaves = 2;
    public float Lacunarity = 2;
    public float Gain = 0.5f;
    public float WeightedStrength = 0f;
    public float PingPongStrength = 0f;

    [Header("Cellular")]
    public FastNoiseLite.CellularDistanceFunction DistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
    public FastNoiseLite.CellularReturnType ReturnType = FastNoiseLite.CellularReturnType.Distance;
    public float Jitter = 1f;

    public override void ValidateValues()
    {
        Frequency = Mathf.Max(Frequency, 0.000001f);
        Octaves = Mathf.Max(Octaves, 1);
        Lacunarity = Mathf.Max(Lacunarity, 0);
    }
}
