using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Noise")]
public class NoiseSettings : VariablePreset
{
    [Header("General")]
    public float Frequency = 0.05f;
    public FastNoiseLite.NoiseType NoiseType;
    public Vector2 Offset = Vector2.zero;

    [Header("Fractal")]
    public FastNoiseLite.FractalType FractalType;
    public int Octaves = 2;
    public float Lacunarity = 2;
    public float Gain = 0.5f;
    public float WeightedStrength = 0f;

    [Header("Cellular")]
    public FastNoiseLite.CellularDistanceFunction DistanceFunction = FastNoiseLite.CellularDistanceFunction.EuclideanSq;
    public FastNoiseLite.CellularReturnType ReturnType = FastNoiseLite.CellularReturnType.Distance;
    public float Jitter = 1f;

    //[Header("Domain Warp")]
    //public FastNoiseLite.DomainWarpType DomainWarp = FastNoiseLite.DomainWarpType.

    //[Header("Domain Warp Fractal")]
    //public FastNoiseLite.DomainWarpType

    public override void ValidateValues()
    {
        Frequency = Mathf.Max(Frequency, 0.0001f);
        Octaves = Mathf.Max(Octaves, 1);
        Lacunarity = Mathf.Max(Lacunarity, 1);
    }
}
