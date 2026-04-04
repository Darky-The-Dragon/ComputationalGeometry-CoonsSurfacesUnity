using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SingleCoonsPatchGenerator : MonoBehaviour
{
    [Header("Reference Surface")] public ReferenceSurfaceGenerator referenceSurface;

    [Header("Patch Domain In World Space")]
    public float xMin = -2f;

    public float xMax = 2f;
    public float zMin = -2f;
    public float zMax = 2f;

    [Header("Patch Tessellation")] [Min(2)]
    public int patchResolution = 20;

    [Header("Display")] public float verticalOffset = 0.03f;

    private Mesh mesh;

    private void Start()
    {
        GeneratePatch();
    }

    public void GeneratePatch()
    {
        if (referenceSurface == null)
            return;

        if (patchResolution < 2)
            patchResolution = 2;

        var meshFilter = GetComponent<MeshFilter>();

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Single Coons Patch Mesh";
        }
        else
        {
            mesh.Clear();
        }

        mesh.indexFormat = IndexFormat.UInt32;
        meshFilter.sharedMesh = mesh;

        var vertCount = patchResolution * patchResolution;
        var vertices = new Vector3[vertCount];
        var uv = new Vector2[vertCount];
        var triangles = new int[(patchResolution - 1) * (patchResolution - 1) * 6];

        var p00 = EvaluateReference(xMin, zMin);
        var p10 = EvaluateReference(xMax, zMin);
        var p01 = EvaluateReference(xMin, zMax);
        var p11 = EvaluateReference(xMax, zMax);

        for (var vIndex = 0; vIndex < patchResolution; vIndex++)
        {
            var v = (float)vIndex / (patchResolution - 1);

            for (var uIndex = 0; uIndex < patchResolution; uIndex++)
            {
                var u = (float)uIndex / (patchResolution - 1);
                var i = vIndex * patchResolution + uIndex;

                var c0 = BottomCurve(u);
                var c1 = TopCurve(u);
                var d0 = LeftCurve(v);
                var d1 = RightCurve(v);

                var bilinearCorners =
                    (1f - u) * (1f - v) * p00 +
                    u * (1f - v) * p10 +
                    (1f - u) * v * p01 +
                    u * v * p11;

                var coonsPoint =
                    (1f - v) * c0 +
                    v * c1 +
                    (1f - u) * d0 +
                    u * d1 -
                    bilinearCorners;

                coonsPoint.y += verticalOffset;

                vertices[i] = coonsPoint;
                uv[i] = new Vector2(u, v);
            }
        }

        var t = 0;

        for (var v = 0; v < patchResolution - 1; v++)
        for (var u = 0; u < patchResolution - 1; u++)
        {
            var i = v * patchResolution + u;

            triangles[t++] = i;
            triangles[t++] = i + patchResolution;
            triangles[t++] = i + 1;

            triangles[t++] = i + 1;
            triangles[t++] = i + patchResolution;
            triangles[t++] = i + patchResolution + 1;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 EvaluateReference(float x, float z)
    {
        return referenceSurface.EvaluatePoint(x, z);
    }

    private Vector3 BottomCurve(float u)
    {
        var x = Mathf.Lerp(xMin, xMax, u);
        return EvaluateReference(x, zMin);
    }

    private Vector3 TopCurve(float u)
    {
        var x = Mathf.Lerp(xMin, xMax, u);
        return EvaluateReference(x, zMax);
    }

    private Vector3 LeftCurve(float v)
    {
        var z = Mathf.Lerp(zMin, zMax, v);
        return EvaluateReference(xMin, z);
    }

    private Vector3 RightCurve(float v)
    {
        var z = Mathf.Lerp(zMin, zMax, v);
        return EvaluateReference(xMax, z);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        EditorApplication.delayCall -= DelayedGenerate;
        EditorApplication.delayCall += DelayedGenerate;
    }

    private void DelayedGenerate()
    {
        if (this == null) return;
        GeneratePatch();
    }
#endif
}