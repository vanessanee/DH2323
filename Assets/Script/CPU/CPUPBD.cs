using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Linq;
using Assets.script;
using UnityEngine.Rendering;
using UnityEngine.Networking;

public class CPUPBD : MonoBehaviour
{
    public enum MyModel
    {
        IcoSphere_low,
        Torus,
        Bunny,
        Armadillo,
    };

    [Header("3D model")]
    public MyModel model;
    [HideInInspector]
    private string modelName;

    [Header("Obj Parameters")]
    public float invMass = 1.0f;
    public float dt = 0.01f;
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    public int iteration = 1;

    [Header("Distance Constraint Parameters")]
    public float stretchStiffness = 1.0f;
    public float compressStiffness = 1.0f;
    [Header("Bending Constraint Parameters")]
    public float bendingStiffness = 1.0f;
    [Header("Volume Constraint Parameters")]
    public float volumeStiffness = 1.0f;

    [Header("Collision")]
    public float floorY = 0.0f;
    public GameObject[] collidableObjects;

    // ---------------------------------------------------------------
    // VR INTERACTION
    // ---------------------------------------------------------------
    public void ApplyForceAtPoint(Vector3 worldPos, Vector3 velocity, float radius)
    {
        if (Positions == null || !simReady) return;
        float radiusSq = radius * radius;
        for (int i = 0; i < nodeCount; i++)
        {
            if ((Positions[i] - worldPos).sqrMagnitude < radiusSq)
            {
                Velocities[i] += velocity;
                // Also directly pull nodes toward controller position
                Positions[i] += velocity * dt;
                ProjectPositions[i] = Positions[i];
            }
        }
    }

    public void ApplyForceToAll(Vector3 velocity)
    {
        if (Positions == null || !simReady) return;
        for (int i = 0; i < nodeCount; i++)
            Velocities[i] += velocity;
    }

    public void ResetToRestState()
    {
        if (Positions == null || !simReady) return;
        for (int i = 0; i < nodeCount; i++)
        {
            Positions[i] = initialPositions[i];
            ProjectPositions[i] = initialPositions[i];
            Velocities[i] = Vector3.zero;
            Forces[i] = Vector3.zero;
        }
    }

    // ---------------------------------------------------------------
    // Private fields
    // ---------------------------------------------------------------
    private bool simReady = false;

    private int nodeCount;
    private int springCount;
    private int triCount;
    private int tetCount;
    private int bendingCount;

    Vector3[] Positions;
    Vector3[] initialPositions;
    Vector3[] ProjectPositions;
    Vector3[] WorldPositions;
    Vector3[] Velocities;
    Vector3[] Forces;
    Vector3[] Normals;
    List<Spring> distanceConstraints = new List<Spring>();
    List<Triangle> triangles = new List<Triangle>();
    List<Tetrahedron> tetrahedrons = new List<Tetrahedron>();
    List<Bending> bendingConstraints = new List<Bending>();

    Vector3[] DeltaPos;
    int[] deltaCounter;

    ComputeBuffer vertsBuff = null;
    ComputeBuffer triBuffer = null;

    [Header("Rendering Parameters")]
    public Shader renderingShader;
    public Color matColor;

    [HideInInspector]
    private Material material;

    [Header("Label Data")]
    public bool renderVolumeText;
    public string Text;
    public int xOffset;
    public int yOffset;
    public int fontSize;
    public Color textColor = Color.white;
    private Rect rectPos;

    struct vertData
    {
        public Vector3 pos;
        public Vector2 uvs;
        public Vector3 norms;
    };
    int[] triArray;
    vertData[] vDataArray;
    float totalVolume;

    // ---------------------------------------------------------------
    // Startup — coroutine so Android file loading works
    // ---------------------------------------------------------------
    void SelectModelName()
    {
        switch (model)
        {
            case MyModel.IcoSphere_low: modelName = "icosphere_low.1"; break;
            case MyModel.Torus:         modelName = "torus.1";          break;
            case MyModel.Bunny:         modelName = "bunny.1";          break;
            case MyModel.Armadillo:     modelName = "Armadillo.1";      break;
        }
    }

    void Start()
    {
        SelectModelName();
        StartCoroutine(LoadAndSetup());
    }

    IEnumerator LoadAndSetup()
    {
        string[] extensions = { ".node", ".ele", ".face", ".springinterior", ".bending" };
        string basePath = Application.streamingAssetsPath + "/TetGen-Model/" + modelName;
        string tempPath = Application.temporaryCachePath + "/TetGen-Model/";
        Directory.CreateDirectory(tempPath);

        foreach (string ext in extensions)
        {
            string url = basePath + ext;
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
                File.WriteAllText(tempPath + modelName + ext, www.downloadHandler.text);
            else
                Debug.LogError("Failed to load: " + url + " — " + www.error);
        }

        // Now load with StreamReader from temp path (works on Android)
        LoadTetModel.LoadData(tempPath + modelName, gameObject);

        setupMeshData();

        material = new Material(renderingShader);
        material.color = matColor;
        setupShader();
        setBuffData();
        totalVolume = computeObjectVolume();
        simReady = true;

        Debug.Log("PBD ready. Nodes: " + nodeCount);
    }

    private void setupMeshData()
    {
        Positions = LoadTetModel.positions.ToArray();
        triangles = LoadTetModel.triangles;
        distanceConstraints = LoadTetModel.springs;
        triArray = LoadTetModel.triangleArr.ToArray();
        tetrahedrons = LoadTetModel.tetrahedrons;
        bendingConstraints = LoadTetModel.bendings;

        nodeCount    = Positions.Length;
        springCount  = distanceConstraints.Count;
        triCount     = triangles.Count;
        tetCount     = tetrahedrons.Count;
        bendingCount = bendingConstraints.Count;

        initialPositions = new Vector3[nodeCount];
        Array.Copy(Positions, initialPositions, nodeCount);

        WorldPositions   = new Vector3[nodeCount];
        ProjectPositions = LoadTetModel.positions.ToArray();
        Velocities       = new Vector3[nodeCount];
        Forces           = new Vector3[nodeCount];
        DeltaPos         = new Vector3[nodeCount];
        deltaCounter     = new int[nodeCount];

        vDataArray = new vertData[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            vDataArray[i]       = new vertData();
            vDataArray[i].pos   = Positions[i];
            vDataArray[i].norms = Vector3.zero;
            vDataArray[i].uvs   = Vector3.zero;
        }

        triBuffer = new ComputeBuffer(triArray.Length, sizeof(int), ComputeBufferType.Default);
        vertsBuff = new ComputeBuffer(vDataArray.Length, 8 * sizeof(float), ComputeBufferType.Default);
        LoadTetModel.ClearData();

        Debug.Log("node count: " + nodeCount);
        Debug.Log("stretch constraint: " + springCount);
        Debug.Log("bending constraint: " + bendingCount);
        Debug.Log("volume constraint: " + tetCount);
    }

    private void setupShader()
    {
        material.SetBuffer(Shader.PropertyToID("vertsBuff"), vertsBuff);
        material.SetBuffer(Shader.PropertyToID("triBuff"), triBuffer);
    }

    private void setBuffData()
    {
        vertsBuff.SetData(vDataArray);
        triBuffer.SetData(triArray);
        Matrix4x4 trs = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
        material.SetMatrix("TRSMatrix", trs);
        material.SetMatrix("invTRSMatrix", trs.inverse);
    }

    // ---------------------------------------------------------------
    // PBD Solver
    // ---------------------------------------------------------------
    void addExternalForce(Vector3 force)
    {
        for (int i = 0; i < nodeCount; i++)
            Velocities[i] += (force / invMass) * dt;
    }

    void addExplicitEuler()
    {
        for (int i = 0; i < nodeCount; i++)
            ProjectPositions[i] = Positions[i] + Velocities[i] * dt;
    }

    void collisionDetectionAndResponse()
{
    for (int i = 0; i < nodeCount; i++)
    {
        if (ProjectPositions[i].y < floorY)
        {
            ProjectPositions[i].y = floorY + 0.01f;
            Velocities[i].y = 0f;  // only zero Y, not X and Z
        }
    }
}

    void satisfyDistanceConstraint()
    {
        for (int i = 0; i < springCount; i++)
        {
            Spring  constraint = distanceConstraints[i];
            int     i1         = constraint.i1;
            int     i2         = constraint.i2;
            float   restLength = constraint.RestLength;
            Vector3 pi         = ProjectPositions[i1];
            Vector3 pj         = ProjectPositions[i2];
            float   d          = Vector3.Distance(pi, pj);
            Vector3 n          = (pi - pj).normalized;
            float   wi         = invMass;
            float   wj         = invMass;
            float   stiffness  = d < restLength ? compressStiffness : stretchStiffness;
            Vector3 deltaP1    = stiffness * wi / (wi + wj) * (d - restLength) * n;
            Vector3 deltaP2    = stiffness * wj / (wi + wj) * (d - restLength) * n;
            ProjectPositions[i1] -= deltaP1;
            ProjectPositions[i2] += deltaP2;
        }
    }

    void satisfyBendingConstraint()
    {
        for (int i = 0; i < bendingCount; i++)
        {
            Bending bending    = bendingConstraints[i];
            Vector3 p0         = ProjectPositions[bending.index0];
            Vector3 p1         = ProjectPositions[bending.index1];
            Vector3 p2         = ProjectPositions[bending.index2];
            Vector3 p3         = ProjectPositions[bending.index3];
            Vector3 wing       = p3 - p2;
            float   wingLength = wing.magnitude;
            if (wingLength >= 1e-7)
            {
                Vector3 n1 = Vector3.Cross(p2 - p0, p3 - p0); n1 /= n1.sqrMagnitude;
                Vector3 n2 = Vector3.Cross(p3 - p1, p2 - p1); n2 /= n2.sqrMagnitude;
                float   invWL = 1.0f / wingLength;
                Vector3 q0 = wingLength * n1;
                Vector3 q1 = wingLength * n2;
                Vector3 q2 = Vector3.Dot(p0 - p3, wing) * invWL * n1 + Vector3.Dot(p1 - p3, wing) * invWL * n2;
                Vector3 q3 = Vector3.Dot(p2 - p0, wing) * invWL * n1 + Vector3.Dot(p2 - p1, wing) * invWL * n2;
                n1.Normalize(); n2.Normalize();
                float d = Mathf.Clamp(Vector3.Dot(n1, n2), -1f, 1f);
                float currentAngle = Mathf.Acos(d);
                float lamda = invMass * (q0.sqrMagnitude + q1.sqrMagnitude + q2.sqrMagnitude + q3.sqrMagnitude);
                if (lamda != 0f)
                {
                    lamda = (currentAngle - bending.restAngle) / lamda * bendingStiffness;
                    if (Vector3.Dot(Vector3.Cross(n1, n2), wing) > 0f) lamda = -lamda;
                    ProjectPositions[bending.index0] -= invMass * lamda * q0;
                    ProjectPositions[bending.index1] -= invMass * lamda * q1;
                    ProjectPositions[bending.index2] -= invMass * lamda * q2;
                    ProjectPositions[bending.index3] -= invMass * lamda * q3;
                }
            }
        }
    }

    void satisfyVolumeConstraint()
    {
        for (int i = 0; i < tetCount; i++)
        {
            Tetrahedron t  = tetrahedrons[i];
            Vector3 p0     = ProjectPositions[t.i1];
            Vector3 p1     = ProjectPositions[t.i2];
            Vector3 p2     = ProjectPositions[t.i3];
            Vector3 p3     = ProjectPositions[t.i4];
            float volume   = computeTetraVolume(p0, p1, p2, p3);
            Vector3 grad0  = Vector3.Cross(p1 - p2, p3 - p2);
            Vector3 grad1  = Vector3.Cross(p2 - p0, p3 - p0);
            Vector3 grad2  = Vector3.Cross(p0 - p1, p3 - p1);
            Vector3 grad3  = Vector3.Cross(p1 - p0, p2 - p0);
            float lambda   = grad0.sqrMagnitude + grad1.sqrMagnitude + grad2.sqrMagnitude + grad3.sqrMagnitude;
            lambda         = volumeStiffness * (volume - t.RestVolume) / lambda;
            ProjectPositions[t.i1] += -lambda * grad0;
            ProjectPositions[t.i2] += -lambda * grad1;
            ProjectPositions[t.i3] += -lambda * grad2;
            ProjectPositions[t.i4] += -lambda * grad3;
        }
    }

    void updatePositions()
    {
        for (int i = 0; i < nodeCount; i++)
        {
            Velocities[i]     = (ProjectPositions[i] - Positions[i]) / dt;
            Positions[i]      = ProjectPositions[i];
            vDataArray[i].pos = Positions[i];
        }
    }

    void PBDSolving()
    {
        addExternalForce(gravity);
        addExplicitEuler();
        for (int j = 0; j < iteration; j++)
        {
            satisfyDistanceConstraint();
            satisfyBendingConstraint();
            satisfyVolumeConstraint();
            collisionDetectionAndResponse();
        }
        updatePositions();
    }

    void computeVertexNormal()
    {
        for (int i = 0; i < triCount; i++)
        {
            Vector3 v1 = Positions[triArray[i * 3 + 0]];
            Vector3 v2 = Positions[triArray[i * 3 + 1]];
            Vector3 v3 = Positions[triArray[i * 3 + 2]];
            Vector3 N  = Vector3.Cross(v2 - v1, v3 - v1);
            vDataArray[triArray[i * 3 + 0]].norms += N;
            vDataArray[triArray[i * 3 + 1]].norms += N;
            vDataArray[triArray[i * 3 + 2]].norms += N;
        }
        for (int i = 0; i < nodeCount; i++)
            vDataArray[i].norms = vDataArray[i].norms.normalized;
    }

    void Update()
    {
        if (!simReady) return;
        PBDSolving();
        computeVertexNormal();
        vertsBuff.SetData(vDataArray);
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 100);
        material.SetPass(0);
        Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles,
            triArray.Length, 1, null, null, ShadowCastingMode.On, true, gameObject.layer);
    }

    private float computeTetraVolume(Vector3 i1, Vector3 i2, Vector3 i3, Vector3 i4)
    {
        return 1.0f / 6.0f
            * (i3.x*i2.y*i1.z - i4.x*i2.y*i1.z - i2.x*i3.y*i1.z + i4.x*i3.y*i1.z
             + i2.x*i4.y*i1.z - i3.x*i4.y*i1.z - i3.x*i1.y*i2.z + i4.x*i1.y*i2.z
             + i1.x*i3.y*i2.z - i4.x*i3.y*i2.z - i1.x*i4.y*i2.z + i3.x*i4.y*i2.z
             + i2.x*i1.y*i3.z - i4.x*i1.y*i3.z - i1.x*i2.y*i3.z + i4.x*i2.y*i3.z
             + i1.x*i4.y*i3.z - i2.x*i4.y*i3.z - i2.x*i1.y*i4.z + i3.x*i1.y*i4.z
             + i1.x*i2.y*i4.z - i3.x*i2.y*i4.z - i1.x*i3.y*i4.z + i2.x*i3.y*i4.z);
    }

    float computeObjectVolume()
    {
        float volume = 0f;
        foreach (Tetrahedron tet in tetrahedrons)
            volume += computeTetraVolume(Positions[tet.i1], Positions[tet.i2],
                                         Positions[tet.i3], Positions[tet.i4]);
        return volume;
    }

    private void OnGUI()
    {
        if (!renderVolumeText) return;
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        rectPos = new Rect(0 + xOffset, yOffset, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize  = h * 2 / 50;
        Color col;
        if (ColorUtility.TryParseHtmlString("#FFED00", out col))
            style.normal.textColor = col;
        float currVolume = computeObjectVolume();
        float vLost = totalVolume == 0f ? 0f : (currVolume / totalVolume) * 100f;
        GUI.Label(rectPos, string.Format("Volume: {0:0.00} %", vLost), style);
    }

    private void OnDestroy()
    {
        vertsBuff?.Dispose();
        triBuffer?.Dispose();
    }
}