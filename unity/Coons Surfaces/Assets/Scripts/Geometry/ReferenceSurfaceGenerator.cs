using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ReferenceSurfaceGenerator : MonoBehaviour
{
    [Header("Surface Settings")] [Min(2)] public int resolution = 100;

    public float size = 10f;
    public float heightScale = 2f;

    [Header("Surface Mode")]
    public SurfaceFunctionProvider.SurfaceMode surfaceMode = SurfaceFunctionProvider.SurfaceMode.AnalyticWaves;

    [Header("Noise Settings")] [Min(0.0001f)]
    public float noiseScale = 0.25f;

    public float noiseOffsetX;
    public float noiseOffsetZ;

    private Mesh mesh;

    public float HalfSize => size * 0.5f;
    public float MinX => -HalfSize;
    public float MaxX => HalfSize;
    public float MinZ => -HalfSize;
    public float MaxZ => HalfSize;

    private void Start()
    {
        GenerateSurface();
    }

    public event Action OnSurfaceRegenerated;

    public void GenerateSurface()
    {
        if (resolution < 2) resolution = 2;

        var meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Reference Surface Mesh";
        }
        else
        {
            mesh.Clear();
        }

        mesh.indexFormat = IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        var vertCount = resolution * resolution;
        var vertices = new Vector3[vertCount];
        var uv = new Vector2[vertCount];
        var triangles = new int[(resolution - 1) * (resolution - 1) * 6];

        var step = size / (resolution - 1);

        for (var z = 0; z < resolution; z++)
        for (var x = 0; x < resolution; x++)
        {
            var i = z * resolution + x;

            var px = x * step + MinX;
            var pz = z * step + MinZ;

            vertices[i] = EvaluatePoint(px, pz);
            uv[i] = new Vector2((float)x / (resolution - 1), (float)z / (resolution - 1));
        }

        var t = 0;

        for (var z = 0; z < resolution - 1; z++)
        for (var x = 0; x < resolution - 1; x++)
        {
            var i = z * resolution + x;

            triangles[t++] = i;
            triangles[t++] = i + resolution;
            triangles[t++] = i + 1;

            triangles[t++] = i + 1;
            triangles[t++] = i + resolution;
            triangles[t++] = i + resolution + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        OnSurfaceRegenerated?.Invoke();
    }

    public float EvaluateHeight(float x, float z)
    {
        return SurfaceFunctionProvider.Evaluate(
            x,
            z,
            surfaceMode,
            heightScale,
            noiseScale,
            noiseOffsetX,
            noiseOffsetZ
        );
    }

    public Vector3 EvaluatePoint(float x, float z)
    {
        return new Vector3(x, EvaluateHeight(x, z), z);
    }

    public void RandomizeNoiseOffsets()
    {
        noiseOffsetX = Random.Range(-1000f, 1000f);
        noiseOffsetZ = Random.Range(-1000f, 1000f);
        GenerateSurface();
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
        GenerateSurface();
    }
#endif
}