using UnityEngine;

public class SurfaceApproximationAnalyzer : MonoBehaviour
{
    [Header("References")]
    public ReferenceSurfaceGenerator referenceSurface;
    public CoonsGridGenerator coonsGrid;

    [Header("Sampling")]
    [Min(2)] public int sampleResolutionX = 50;
    [Min(2)] public int sampleResolutionZ = 50;

    [Header("Options")]
    public bool analyzeOnStart = true;
    public bool logResults = true;

    [Header("Results (Read Only)")]
    [SerializeField] private float meanError;
    [SerializeField] private float maxError;
    [SerializeField] private float rmse;
    [SerializeField] private int sampleCount;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        if (analyzeOnStart)
            Analyze();
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

        float minX = referenceSurface.MinX;
        float maxX = referenceSurface.MaxX;
        float minZ = referenceSurface.MinZ;
        float maxZ = referenceSurface.MaxZ;

        float sumAbsError = 0f;
        float sumSquaredError = 0f;
        float worstError = 0f;
        int count = 0;

        for (int z = 0; z < sampleResolutionZ; z++)
        {
            float tz = (float)z / (sampleResolutionZ - 1);
            float worldZ = Mathf.Lerp(minZ, maxZ, tz);

            for (int x = 0; x < sampleResolutionX; x++)
            {
                float tx = (float)x / (sampleResolutionX - 1);
                float worldX = Mathf.Lerp(minX, maxX, tx);

                float referenceHeight = referenceSurface.EvaluateHeight(worldX, worldZ);
                float approxHeight = coonsGrid.EvaluateApproximationHeight(worldX, worldZ);

                float error = Mathf.Abs(referenceHeight - approxHeight);

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
        {
            Debug.Log(
                $"[SurfaceApproximationAnalyzer] Samples={sampleCount} | " +
                $"Mean Error={meanError:F6} | Max Error={maxError:F6} | RMSE={rmse:F6}"
            );
        }
    }

    public float GetMeanError() => meanError;
    public float GetMaxError() => maxError;
    public float GetRMSE() => rmse;
    public int GetSampleCount() => sampleCount;
}