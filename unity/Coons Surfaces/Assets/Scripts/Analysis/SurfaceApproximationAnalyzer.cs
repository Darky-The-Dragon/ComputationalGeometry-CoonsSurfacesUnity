using UnityEngine;

public class SurfaceApproximationAnalyzer : MonoBehaviour
{
    [Header("References")] public ReferenceSurfaceGenerator referenceSurface;

    public CoonsGridGenerator coonsGrid;

    [Header("Sampling")] [Min(2)] public int sampleResolutionX = 50;

    [Min(2)] public int sampleResolutionZ = 50;

    [Header("Options")] public bool analyzeOnStart = true;

    public bool logResults = true;

    [Header("Results (Read Only)")] [SerializeField]
    private float meanError;

    [SerializeField] private float maxError;
    [SerializeField] private float rmse;
    [SerializeField] private int sampleCount;

    private void Start()
    {
        if (analyzeOnStart)
            Analyze();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (referenceSurface != null)
        {
            referenceSurface.OnSurfaceRegenerated -= HandleDataChanged;
            referenceSurface.OnSurfaceRegenerated += HandleDataChanged;
        }

        if (coonsGrid != null)
        {
            coonsGrid.OnGridRegenerated -= HandleDataChanged;
            coonsGrid.OnGridRegenerated += HandleDataChanged;
        }
    }

    private void Unsubscribe()
    {
        if (referenceSurface != null)
            referenceSurface.OnSurfaceRegenerated -= HandleDataChanged;

        if (coonsGrid != null)
            coonsGrid.OnGridRegenerated -= HandleDataChanged;
    }

    private void HandleDataChanged()
    {
        Analyze();
    }

    public void Analyze()
    {
        if (referenceSurface == null || coonsGrid == null)
        {
            Debug.LogWarning("[SurfaceApproximationAnalyzer] Missing referenceSurface or coonsGrid.");
            return;
        }

        if (sampleResolutionX < 2) sampleResolutionX = 2;
        if (sampleResolutionZ < 2) sampleResolutionZ = 2;

        var minX = referenceSurface.MinX;
        var maxX = referenceSurface.MaxX;
        var minZ = referenceSurface.MinZ;
        var maxZ = referenceSurface.MaxZ;

        var sumAbsError = 0f;
        var sumSquaredError = 0f;
        var worstError = 0f;
        var count = 0;

        for (var z = 0; z < sampleResolutionZ; z++)
        {
            var tz = (float)z / (sampleResolutionZ - 1);
            var worldZ = Mathf.Lerp(minZ, maxZ, tz);

            for (var x = 0; x < sampleResolutionX; x++)
            {
                var tx = (float)x / (sampleResolutionX - 1);
                var worldX = Mathf.Lerp(minX, maxX, tx);

                var referenceHeight = referenceSurface.EvaluateHeight(worldX, worldZ);
                var approxHeight = coonsGrid.EvaluateApproximationHeight(worldX, worldZ);

                var error = Mathf.Abs(referenceHeight - approxHeight);

                sumAbsError += error;
                sumSquaredError += error * error;
                if (error > worstError)
                    worstError = error;

                count++;
            }
        }

        sampleCount = count;
        meanError = sumAbsError / count;
        maxError = worstError;
        rmse = Mathf.Sqrt(sumSquaredError / count);

        if (logResults)
            Debug.Log(
                $"[SurfaceApproximationAnalyzer] Samples={sampleCount} | " +
                $"Mean Error={meanError:F6} | Max Error={maxError:F6} | RMSE={rmse:F6}"
            );
    }

    public float GetMeanError()
    {
        return meanError;
    }

    public float GetMaxError()
    {
        return maxError;
    }

    public float GetRMSE()
    {
        return rmse;
    }

    public int GetSampleCount()
    {
        return sampleCount;
    }
}