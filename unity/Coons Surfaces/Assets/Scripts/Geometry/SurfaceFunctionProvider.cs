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
        return Mathf.Sin(x * 0.4f) * Mathf.Cos(z * 0.4f) * heightScale
             + 0.3f * Mathf.Sin(x * 1.5f) * Mathf.Sin(z * 1.5f);
    }

    private static float EvaluatePerlinNoise(
        float x,
        float z,
        float heightScale,
        float noiseScale,
        float noiseOffsetX,
        float noiseOffsetZ)
    {
        float nx = x * noiseScale + noiseOffsetX;
        float nz = z * noiseScale + noiseOffsetZ;

        float n = Mathf.PerlinNoise(nx, nz);
        float centered = (n - 0.5f) * 2f;

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
        float nx = x * noiseScale + noiseOffsetX;
        float nz = z * noiseScale + noiseOffsetZ;

        float n1 = Mathf.PerlinNoise(nx, nz);
        float n2 = Mathf.PerlinNoise(nx * 2f, nz * 2f) * 0.5f;
        float n3 = Mathf.PerlinNoise(nx * 4f, nz * 4f) * 0.25f;

        float combined = (n1 + n2 + n3) / 1.75f;
        float centered = (combined - 0.5f) * 2f;

        return centered * heightScale;
    }
}
