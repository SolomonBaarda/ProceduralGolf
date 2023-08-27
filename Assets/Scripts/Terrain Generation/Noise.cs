using System;

public static class Noise
{
    public static int RandomSeed => Environment.TickCount.ToString().GetHashCode();

    public static float[] GetNoise(NoiseSettings s, int seed, int width, int height, out float min, out float max)
    {
        s.ValidateValues();

        FastNoiseLite n = new FastNoiseLite(seed);

        n.SetNoiseType(s.NoiseType);
        n.SetRotationType3D(s.RotationType);
        n.SetFractalType(s.FractalType);
        n.SetFractalOctaves(s.Octaves);
        n.SetFractalLacunarity(s.Lacunarity);
        n.SetFrequency(s.Frequency);
        n.SetFractalGain(s.Gain);
        n.SetFractalWeightedStrength(s.WeightedStrength);
        n.SetCellularDistanceFunction(s.DistanceFunction);
        n.SetCellularReturnType(s.ReturnType);
        n.SetCellularJitter(s.Jitter);
        n.SetFractalPingPongStrength(s.PingPongStrength);

        float[] noise = new float[width * height];

        min = float.MaxValue;
        max = float.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sample = n.GetNoise(x, y);

                if (sample < min) min = sample;
                if (sample > max) max = sample;

                noise[(y * width) + x] = sample;
            }
        }

        return noise;
    }
}


