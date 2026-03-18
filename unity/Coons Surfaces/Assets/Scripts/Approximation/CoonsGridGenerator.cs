using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CoonsGridGenerator : MonoBehaviour
{
    [Header("Reference Surface")]
    public ReferenceSurfaceGenerator referenceSurface;

    [Header("Grid Subdivision")]
    [Min(1)] public int gridResolutionX = 4;
    [Min(1)] public int gridResolutionZ = 4;

    [Header("Patch Tessellation")]
    [Min(2)] public int patchResolution = 12;

    [Header("Display")]
    public float verticalOffset = 0.03f;

    [Header("Heatmap")]
    public bool enableHeatmap = true;
    public float heatmapMaxError = 1f;

    public event Action OnGridRegenerated;

    private Mesh mesh;

    private void OnEnable()
    {
        SubscribeToReference();
    }

    private void OnDisable()
    {
        UnsubscribeFromReference();
    }

    private void Start()
    {
        GenerateGrid();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorApplication.delayCall -= DelayedGenerate;
        EditorApplication.delayCall += DelayedGenerate;
    }

    private void DelayedGenerate()
    {
        if (this == null) return;
        SubscribeToReference();
        GenerateGrid();
    }
#endif

    private void SubscribeToReference()
    {
        if (referenceSurface != null)
        {
            referenceSurface.OnSurfaceRegenerated -= HandleReferenceSurfaceRegenerated;
            referenceSurface.OnSurfaceRegenerated += HandleReferenceSurfaceRegenerated;
        }
    }

    private void UnsubscribeFromReference()
    {
        if (referenceSurface != null)
        {
            referenceSurface.OnSurfaceRegenerated -= HandleReferenceSurfaceRegenerated;
        }
    }

    private void HandleReferenceSurfaceRegenerated()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        if (referenceSurface == null)
            return;

        if (gridResolutionX < 1) gridResolutionX = 1;
        if (gridResolutionZ < 1) gridResolutionZ = 1;
        if (patchResolution < 2) patchResolution = 2;
        if (heatmapMaxError <= 0f) heatmapMaxError = 0.0001f;

        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Coons Grid Mesh";
        }
        else
        {
            mesh.Clear();
        }

        mesh.indexFormat = IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        int vertsPerPatch = patchResolution * patchResolution;
        int trisPerPatch = (patchResolution - 1) * (patchResolution - 1) * 6;
        int patchCount = gridResolutionX * gridResolutionZ;

        Vector3[] vertices = new Vector3[vertsPerPatch * patchCount];
        Vector2[] uv = new Vector2[vertsPerPatch * patchCount];
        Color[] colors = new Color[vertsPerPatch * patchCount];
        int[] triangles = new int[trisPerPatch * patchCount];

        int vertexOffset = 0;
        int triangleOffset = 0;

        for (int gz = 0; gz < gridResolutionZ; gz++)
        {
            for (int gx = 0; gx < gridResolutionX; gx++)
            {
                float xMin = referenceSurface.MinX + gx * CellSizeX;
                float xMax = xMin + CellSizeX;
                float zMin = referenceSurface.MinZ + gz * CellSizeZ;
                float zMax = zMin + CellSizeZ;

                BuildPatch(
                    xMin, xMax,
                    zMin, zMax,
                    vertices, uv, colors, triangles,
                    ref vertexOffset,
                    ref triangleOffset
                );
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        OnGridRegenerated?.Invoke();
    }

    public Vector3 EvaluateApproximationPoint(float x, float z)
    {
        if (referenceSurface == null)
            return Vector3.zero;

        float clampedX = Mathf.Clamp(x, referenceSurface.MinX, referenceSurface.MaxX);
        float clampedZ = Mathf.Clamp(z, referenceSurface.MinZ, referenceSurface.MaxZ);

        int gx = Mathf.Min(
            Mathf.FloorToInt((clampedX - referenceSurface.MinX) / CellSizeX),
            gridResolutionX - 1
        );

        int gz = Mathf.Min(
            Mathf.FloorToInt((clampedZ - referenceSurface.MinZ) / CellSizeZ),
            gridResolutionZ - 1
        );

        float xMin = referenceSurface.MinX + gx * CellSizeX;
        float xMax = xMin + CellSizeX;
        float zMin = referenceSurface.MinZ + gz * CellSizeZ;
        float zMax = zMin + CellSizeZ;

        float u = Mathf.InverseLerp(xMin, xMax, clampedX);
        float v = Mathf.InverseLerp(zMin, zMax, clampedZ);

        return EvaluatePatchPoint(xMin, xMax, zMin, zMax, u, v);
    }

    public float EvaluateApproximationHeight(float x, float z)
    {
        return EvaluateApproximationPoint(x, z).y;
    }

    private float CellSizeX => (referenceSurface.MaxX - referenceSurface.MinX) / gridResolutionX;
    private float CellSizeZ => (referenceSurface.MaxZ - referenceSurface.MinZ) / gridResolutionZ;

    private void BuildPatch(
        float xMin, float xMax,
        float zMin, float zMax,
        Vector3[] vertices,
        Vector2[] uv,
        Color[] colors,
        int[] triangles,
        ref int vertexOffset,
        ref int triangleOffset)
    {
        for (int vIndex = 0; vIndex < patchResolution; vIndex++)
        {
            float v = (float)vIndex / (patchResolution - 1);

            for (int uIndex = 0; uIndex < patchResolution; uIndex++)
            {
                float u = (float)uIndex / (patchResolution - 1);

                Vector3 coonsPoint = EvaluatePatchPoint(xMin, xMax, zMin, zMax, u, v);
                float referenceHeight = referenceSurface.EvaluateHeight(coonsPoint.x, coonsPoint.z);
                float error = Mathf.Abs(referenceHeight - coonsPoint.y);

                Vector3 displayPoint = coonsPoint;
                displayPoint.y += verticalOffset;

                int i = vertexOffset + vIndex * patchResolution + uIndex;
                vertices[i] = displayPoint;

                float patchU = (coonsPoint.x - referenceSurface.MinX) / (referenceSurface.MaxX - referenceSurface.MinX);
                float patchV = (coonsPoint.z - referenceSurface.MinZ) / (referenceSurface.MaxZ - referenceSurface.MinZ);
                uv[i] = new Vector2(patchU, patchV);

                colors[i] = enableHeatmap ? EvaluateHeatColor(error) : Color.white;
            }
        }

        for (int v = 0; v < patchResolution - 1; v++)
        {
            for (int u = 0; u < patchResolution - 1; u++)
            {
                int local = vertexOffset + v * patchResolution + u;

                triangles[triangleOffset++] = local;
                triangles[triangleOffset++] = local + patchResolution;
                triangles[triangleOffset++] = local + 1;

                triangles[triangleOffset++] = local + 1;
                triangles[triangleOffset++] = local + patchResolution;
                triangles[triangleOffset++] = local + patchResolution + 1;
            }
        }

        vertexOffset += patchResolution * patchResolution;
    }

    private Vector3 EvaluatePatchPoint(float xMin, float xMax, float zMin, float zMax, float u, float v)
    {
        Vector3 p00 = referenceSurface.EvaluatePoint(xMin, zMin);
        Vector3 p10 = referenceSurface.EvaluatePoint(xMax, zMin);
        Vector3 p01 = referenceSurface.EvaluatePoint(xMin, zMax);
        Vector3 p11 = referenceSurface.EvaluatePoint(xMax, zMax);

        Vector3 c0 = BottomCurve(xMin, xMax, zMin, u);
        Vector3 c1 = TopCurve(xMin, xMax, zMax, u);
        Vector3 d0 = LeftCurve(xMin, zMin, zMax, v);
        Vector3 d1 = RightCurve(xMax, zMin, zMax, v);

        Vector3 bilinearCorners =
            (1f - u) * (1f - v) * p00 +
            u * (1f - v) * p10 +
            (1f - u) * v * p01 +
            u * v * p11;

        return (1f - v) * c0 +
               v * c1 +
               (1f - u) * d0 +
               u * d1 -
               bilinearCorners;
    }

    private Color EvaluateHeatColor(float error)
    {
        float t = Mathf.Clamp01(error / heatmapMaxError);

        if (t < 0.33f)
        {
            float localT = t / 0.33f;
            return Color.Lerp(Color.blue, Color.green, localT);
        }
        else if (t < 0.66f)
        {
            float localT = (t - 0.33f) / 0.33f;
            return Color.Lerp(Color.green, Color.yellow, localT);
        }
        else
        {
            float localT = (t - 0.66f) / 0.34f;
            return Color.Lerp(Color.yellow, Color.red, localT);
        }
    }

    private Vector3 BottomCurve(float xMin, float xMax, float z, float u)
    {
        float x = Mathf.Lerp(xMin, xMax, u);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 TopCurve(float xMin, float xMax, float z, float u)
    {
        float x = Mathf.Lerp(xMin, xMax, u);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 LeftCurve(float x, float zMin, float zMax, float v)
    {
        float z = Mathf.Lerp(zMin, zMax, v);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 RightCurve(float x, float zMin, float zMax, float v)
    {
        float z = Mathf.Lerp(zMin, zMax, v);
        return referenceSurface.EvaluatePoint(x, z);
    }
}