using UnityEngine;
using System.Collections.Generic;


public class FreeFormDeformer : MonoBehaviour {

    public bool AllowMeshUpdate = false;
    public MeshFilter MorphTargetFilter = null;
    public GameObject ControlPointPrefab;
    public int L = 2, M = 2, N = 2;
    public float UpdateFrequency = 0.05f;

    Mesh MorphTarget;
    Vector3[] originalVertices;
    Vector3[] transformedVertices;
    List<Vector3Param> vertexParams = new List<Vector3Param>();
    GameObject[,,] controlPoints;
    Vector3 S, T, U, origin;
    float elapsedTime = 0f;

    void Start() {
        MorphTarget = MorphTargetFilter.mesh;
        originalVertices = MorphTarget.vertices;
        transformedVertices = new Vector3[originalVertices.Length];
        Parameterize();
    }

    float Binomial(int n, int k) {
        float result = 1f;
        for (int i = 1; i <= k; i++)
            result *= (n - (k - i)) / (float)i;
        return result;
    }

    float Bernstein(int n, int v, float x) {
        return Binomial(n, v) * Mathf.Pow(x, v) * Mathf.Pow(1f - x, n - v);
    }

    void Parameterize() {
        // 1. Find bounding box of mesh
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (Vector3 v in originalVertices) {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        // 2. Define local axes S, T, U along X, Y, Z extents
        origin = min;
        S = new Vector3(max.x - min.x, 0, 0);
        T = new Vector3(0, max.y - min.y, 0);
        U = new Vector3(0, 0, max.z - min.z);

        // 3. For each vertex, calculate its s,t,u coords (0 to 1 along each axis)
        //    then pre-bake the Bernstein polynomials
        foreach (Vector3 vert in originalVertices) {
            Vector3 diff = vert - origin;
            Vector3Param vp = new Vector3Param();
            vp.s = diff.x / S.x;
            vp.t = diff.y / T.y;
            vp.u = diff.z / U.z;

            // Pre-calculate bernstein values for each axis
            vp.bernS = new List<float>();
            vp.bernT = new List<float>();
            vp.bernU = new List<float>();
            for (int i = 0; i <= L; i++) vp.bernS.Add(Bernstein(L, i, vp.s));
            for (int j = 0; j <= M; j++) vp.bernT.Add(Bernstein(M, j, vp.t));
            for (int k = 0; k <= N; k++) vp.bernU.Add(Bernstein(N, k, vp.u));

            vertexParams.Add(vp);
        }

        // 4. Spawn control points in a grid around the mesh
        controlPoints = new GameObject[L+1, M+1, N+1];
        for (int i = 0; i <= L; i++)
            for (int j = 0; j <= M; j++)
                for (int k = 0; k <= N; k++) {
                    Vector3 pos = origin
                        + (i / (float)L) * S
                        + (j / (float)M) * T
                        + (k / (float)N) * U;
                    GameObject cp = Instantiate(ControlPointPrefab, pos, Quaternion.identity);
                    cp.transform.parent = transform;
                    controlPoints[i, j, k] = cp;
                }
    }

    // This is the core FFD formula from the Sederberg & Parry paper
    // It's a triple sum: for each vertex, sum contributions from all control points
    // weighted by their Bernstein polynomial values
    Vector3 FFDPoint(Vector3Param vp) {
        Vector3 result = Vector3.zero;
        for (int i = 0; i <= L; i++)
            for (int j = 0; j <= M; j++)
                for (int k = 0; k <= N; k++)
                    result += vp.bernS[i] * vp.bernT[j] * vp.bernU[k]
                              * controlPoints[i,j,k].transform.localPosition;
        return result;
    }

    void UpdateMesh() {
        elapsedTime = 0f;
        for (int v = 0; v < vertexParams.Count; v++)
            transformedVertices[v] = FFDPoint(vertexParams[v]);
        MorphTarget.vertices = transformedVertices;
        MorphTarget.RecalculateBounds();
        MorphTarget.RecalculateNormals();
    }

    void OnGUI() {
        AllowMeshUpdate = GUI.Toggle(new Rect(10, 10, 200, 25), AllowMeshUpdate, "Enable Deformation");
        GUI.Label(new Rect(10, 35, 300, 20), "Drag the white spheres to deform the mesh");
    }

    void FixedUpdate() {
        elapsedTime += Time.fixedDeltaTime;
        if (AllowMeshUpdate && elapsedTime >= UpdateFrequency)
            UpdateMesh();
    }
}