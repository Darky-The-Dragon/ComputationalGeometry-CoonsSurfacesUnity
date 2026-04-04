using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CoonsGridGenerator : MonoBehaviour
{
    [Header("Reference Surface")] public ReferenceSurfaceGenerator referenceSurface;

    [Header("Grid Subdivision")] [Min(1)] public int gridResolutionX = 4;

    [Min(1)] public int gridResolutionZ = 4;

    [Header("Patch Tessellation")] [Min(2)]
    public int patchResolution = 12;

    [Header("Display")] public float verticalOffset = 0.7f;

    [Header("Heatmap")] public bool enableHeatmap = true;

    public float heatmapMaxError = 1f;

    private Mesh mesh;

    private float CellSizeX => (referenceSurface.MaxX - referenceSurface.MinX) / gridResolutionX;
    private float CellSizeZ => (referenceSurface.MaxZ - referenceSurface.MinZ) / gridResolutionZ;

    private void Start()
    {
        GenerateGrid();
    }

    public event Action OnGridRegenerated;

    public void GenerateGrid()
    {
        if (referenceSurface == null)
            return;

        if (gridResolutionX < 1) gridResolutionX = 1;
        if (gridResolutionZ < 1) gridResolutionZ = 1;
        if (patchResolution < 2) patchResolution = 2;
        if (heatmapMaxError <= 0f) heatmapMaxError = 0.0001f;

        var meshFilter = GetComponent<MeshFilter>();

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

        var vertsPerPatch = patchResolution * patchResolution;
        var trisPerPatch = (patchResolution - 1) * (patchResolution - 1) * 6;
        var patchCount = gridResolutionX * gridResolutionZ;

        var vertices = new Vector3[vertsPerPatch * patchCount];
        var uv = new Vector2[vertsPerPatch * patchCount];
        var colors = new Color[vertsPerPatch * patchCount];
        var triangles = new int[trisPerPatch * patchCount];

        var vertexOffset = 0;
        var triangleOffset = 0;

        for (var gz = 0; gz < gridResolutionZ; gz++)
        for (var gx = 0; gx < gridResolutionX; gx++)
        {
            var xMin = referenceSurface.MinX + gx * CellSizeX;
            var xMax = xMin + CellSizeX;
            var zMin = referenceSurface.MinZ + gz * CellSizeZ;
            var zMax = zMin + CellSizeZ;

            BuildPatch(
                xMin, xMax,
                zMin, zMax,
                vertices, uv, colors, triangles,
                ref vertexOffset,
                ref triangleOffset
            );
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

        var clampedX = Mathf.Clamp(x, referenceSurface.MinX, referenceSurface.MaxX);
        var clampedZ = Mathf.Clamp(z, referenceSurface.MinZ, referenceSurface.MaxZ);

        var gx = Mathf.Min(
            Mathf.FloorToInt((clampedX - referenceSurface.MinX) / CellSizeX),
            gridResolutionX - 1
        );

        var gz = Mathf.Min(
            Mathf.FloorToInt((clampedZ - referenceSurface.MinZ) / CellSizeZ),
            gridResolutionZ - 1
        );

        var xMin = referenceSurface.MinX + gx * CellSizeX;
        var xMax = xMin + CellSizeX;
        var zMin = referenceSurface.MinZ + gz * CellSizeZ;
        var zMax = zMin + CellSizeZ;

        var u = Mathf.InverseLerp(xMin, xMax, clampedX);
        var v = Mathf.InverseLerp(zMin, zMax, clampedZ);

        return EvaluatePatchPoint(xMin, xMax, zMin, zMax, u, v);
    }

    public float EvaluateApproximationHeight(float x, float z)
    {
        return EvaluateApproximationPoint(x, z).y;
    }

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
        for (var vIndex = 0; vIndex < patchResolution; vIndex++)
        {
            var v = (float)vIndex / (patchResolution - 1);

            for (var uIndex = 0; uIndex < patchResolution; uIndex++)
            {
                var u = (float)uIndex / (patchResolution - 1);

                var coonsPoint = EvaluatePatchPoint(xMin, xMax, zMin, zMax, u, v);
                var referenceHeight = referenceSurface.EvaluateHeight(coonsPoint.x, coonsPoint.z);
                var error = Mathf.Abs(referenceHeight - coonsPoint.y);

                var displayPoint = coonsPoint;
                displayPoint.y += verticalOffset;

                var i = vertexOffset + vIndex * patchResolution + uIndex;
                vertices[i] = displayPoint;

                var patchU = (coonsPoint.x - referenceSurface.MinX) / (referenceSurface.MaxX - referenceSurface.MinX);
                var patchV = (coonsPoint.z - referenceSurface.MinZ) / (referenceSurface.MaxZ - referenceSurface.MinZ);
                uv[i] = new Vector2(patchU, patchV);

                colors[i] = enableHeatmap ? EvaluateHeatColor(error) : Color.white;
            }
        }

        for (var v = 0; v < patchResolution - 1; v++)
        for (var u = 0; u < patchResolution - 1; u++)
        {
            var local = vertexOffset + v * patchResolution + u;

            triangles[triangleOffset++] = local;
            triangles[triangleOffset++] = local + patchResolution;
            triangles[triangleOffset++] = local + 1;

            triangles[triangleOffset++] = local + 1;
            triangles[triangleOffset++] = local + patchResolution;
            triangles[triangleOffset++] = local + patchResolution + 1;
        }

        vertexOffset += patchResolution * patchResolution;
    }

    private Vector3 EvaluatePatchPoint(float xMin, float xMax, float zMin, float zMax, float u, float v)
    {
        var p00 = referenceSurface.EvaluatePoint(xMin, zMin);
        var p10 = referenceSurface.EvaluatePoint(xMax, zMin);
        var p01 = referenceSurface.EvaluatePoint(xMin, zMax);
        var p11 = referenceSurface.EvaluatePoint(xMax, zMax);

        var c0 = BottomCurve(xMin, xMax, zMin, u);
        var c1 = TopCurve(xMin, xMax, zMax, u);
        var d0 = LeftCurve(xMin, zMin, zMax, v);
        var d1 = RightCurve(xMax, zMin, zMax, v);

        var bilinearCorners =
            (1f - u) * (1f - v) * p00 +
            u * (1f - v) * p10 +
            (1f - u) * v * p01 +
            u * v * p11;

        return (1f - v) * c0 + v * c1 + (1f - u) * d0 + u * d1 - bilinearCorners;
    }

    private Vector3 BottomCurve(float xMin, float xMax, float z, float u)
    {
        var x = Mathf.Lerp(xMin, xMax, u);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 TopCurve(float xMin, float xMax, float z, float u)
    {
        var x = Mathf.Lerp(xMin, xMax, u);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 LeftCurve(float x, float zMin, float zMax, float v)
    {
        var z = Mathf.Lerp(zMin, zMax, v);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 RightCurve(float x, float zMin, float zMax, float v)
    {
        var z = Mathf.Lerp(zMin, zMax, v);
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Color EvaluateHeatColor(float error)
    {
        var t = Mathf.Clamp01(error / heatmapMaxError);

        if (t < 0.33f)
            return Color.Lerp(Color.blue, Color.green, t / 0.33f);

        if (t < 0.66f)
            return Color.Lerp(Color.green, Color.yellow, (t - 0.33f) / 0.33f);

        return Color.Lerp(Color.yellow, Color.red, (t - 0.66f) / 0.34f);
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
        GenerateGrid();
    }
#endif
}