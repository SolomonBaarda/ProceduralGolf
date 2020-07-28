using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Noise")]
public class NoiseSettings : VariablePreset
{
    public float scale = 50;

    public int octaves = 4;
    [Range(0, 1)]
    public float persistance = .5f;
    public float lacunarity = 2;

    public Vector2 offset = Vector2.zero;

    public override void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}
