using UnityEngine;

public static class SurfaceFunctionProvider
{
    public enum SurfaceMode
    {
        AnalyticWaves,
        PerlinNoise,
        LayeredPerlinNoise
    }

    public static float Evaluate(
        float x,
        float z,
        SurfaceMode mode,
        float heightScale,
        float noiseScale,
        float noiseOffsetX,
        float noiseOffsetZ)
    {
        switch (mode)
        {
            case SurfaceMode.AnalyticWaves:
                return EvaluateAnalyticWaves(x, z, heightScale);

            case SurfaceMode.PerlinNoise:
                return EvaluatePerlinNoise(x, z, heightScale, noiseScale, noiseOffsetX, noiseOffsetZ);

            case SurfaceMode.LayeredPerlinNoise:
                return EvaluateLayeredPerlinNoise(x, z, heightScale, noiseScale, noiseOffsetX, noiseOffsetZ);

            default:
                return EvaluateAnalyticWaves(x, z, heightScale);
        }
    }

    private static float EvaluateAnalyticWaves(float x, float z, float heightScale)
    {
        var waves =
            Mathf.Sin(x * 0.4f) * Mathf.Cos(z * 0.4f) +
            0.3f * Mathf.Sin(x * 1.5f) * Mathf.Sin(z * 1.5f);

        return waves * heightScale;
    }

    private static float EvaluatePerlinNoise(
        float x,
        float z,
        float heightScale,
        float noiseScale,
        float noiseOffsetX,
        float noiseOffsetZ)
    {
        var nx = x * noiseScale + noiseOffsetX;
        var nz = z * noiseScale + noiseOffsetZ;

        var n = Mathf.PerlinNoise(nx, nz);
        var centered = (n - 0.5f) * 2f;

        return centered * heightScale;
    }

    private static float EvaluateLayeredPerlinNoise(
        float x,
        float z,
        float heightScale,
        float noiseScale,
        float noiseOffsetX,
        float noiseOffsetZ)
    {
        var nx = x * noiseScale + noiseOffsetX;
        var nz = z * noiseScale + noiseOffsetZ;

        var n1 = Mathf.PerlinNoise(nx, nz);
        var n2 = Mathf.PerlinNoise(nx * 2f, nz * 2f) * 0.5f;
        var n3 = Mathf.PerlinNoise(nx * 4f, nz * 4f) * 0.25f;

        var combined = (n1 + n2 + n3) / 1.75f;
        var centered = (combined - 0.5f) * 2f;

        return centered * heightScale;
    }
}