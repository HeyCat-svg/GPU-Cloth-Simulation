using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothSimulation : MonoBehaviour {
    
    public struct DispatchDim {
        public int x;
        public int y;
        public int z;
    }

    public struct Anchor_t {
        public int pointIdx;
        public Vector3 relCoord;
    };

    private string spheretag = "sphere";
    private string cubetag = "cube";
    private ClothRender clothRender;

    [Header("Compute Shaders")]
    public ComputeShader nodeUpdateCS;
    public ComputeShader collisionResponseCS;
    public ComputeShader normalCS;
    public ComputeShader tangentCS;
    public ComputeShader accelerationCS;

    [Header("Cloth Attributes")]
    public GameObject clothObj;
    public int verCountPerRow = 30;
    public int verCountPerCol = 30;
    public float gridSize = 0.2f;
    [Range(0.0f, Mathf.PI)] public float bendingAngle = Mathf.PI;
    [Range(1.0f, 5.0f)] public float lineBreakThres = 3.0f;
    [Range(0.0f, 1.0f)] public float damping = 1.0f;
    [Range(1, 500)] public int looper = 1;
    public Vector3 g = new Vector3(0, -9.8f, 0);
    public int iterCount = 3;

    [Header("Cloth Anchors")]
    public Vector2Int[] anchorRowAndCol;
    public Vector4[] anchorRelPos;
    public int anchorCount {
        get {
            if (anchorRowAndCol.Length != anchorRelPos.Length) {
                return 0;
            }
            return anchorRowAndCol.Length;
        }
    }
    
    [Header("Wind Force Attributes")]
    public Vector3 wind_V = new Vector3(0, 0 , 1.5f);
    public float dragCoeff = 0.5f;      // 阻力系数
    public float liftCoeff = 0.5f;      // 升力系数
    public float windNoiseScale = 10;   // WindLocal坐标缩放
    public float windNoiceGridSize = 2;     // 相邻格点宽度
    public float windNoiceStrength = 1;    // 柏林噪声系数
    public float windVibrationStrength = 0.2f;  // 振幅
    
    public ComputeBuffer p0_buffer;
    public ComputeBuffer[] p1_bufferPool;
    public ComputeBuffer TRS_buffer;
    public ComputeBuffer texcoord_buffer;
    public ComputeBuffer tris_buffer;
    public ComputeBuffer normal_buffer;
    public ComputeBuffer tangent_buffer;
    public ComputeBuffer acceleration_buffer;
    public ComputeBuffer linkInfo_buffer;
    public ComputeBuffer anchorRowAndCol_buffer;
    public ComputeBuffer anchorRelPos_buffer;
    public ComputeBuffer bunnyspace_buffer;
    private int p1_bufferPoolSwapIdx = 0;

    // CS Dim
    private DispatchDim NUCS_dispatchDim;
    private DispatchDim CRCS_dispatchDim;

    // CS handles
    private int NUCS_handle;    // node update compute shader
    private int NUCS_lengthHandle;  // 距离约束
    private int NUCS_bendingHandle;     //弯曲约束
    private int NUCS_linkCorrectionHandle;  // 计算边断裂
    private int NUCS_linkCorrectionInvHandle;   // 逆向补全断边信息
    private int CRCS_spherehandle;    // sphere collision
    private int CRCS_cubehandle;    // cube collision
    private int CRCS_innerhandle;    // inner collision
    private int CRCS_swaphandle;
    private int CRCS_bunnyhandle;
    private int NCS_handle;     // 计算法线
    private int TCS_handle;     // 计算切线方向
    private int ACS_handle;     // 计算加速度

    // cloth data
    private Matrix4x4[] clothTRS = new Matrix4x4[2];
    private Anchor_t[] clothAnchors;
    private Vector4[] positions;
    private Vector2[] texcoords;
    private Vector4[] normals;
    private Vector2[] tangents;
    private Vector4[] accelerations;
    private int[] tris;
    private uint[] linkInfos;
    private int triIdx;
    private float deltaT;
    [HideInInspector] public int verCount = 0;
    private int triIdxSize = 0;
    private Vector4 leftFixP, rightFixP;    // 固定点相对坐标

    private bool isInit = false;
    Vector3[] bunnyspace;


    void Start() {
        Init();
        clothRender = GetComponent<ClothRender>();
        clothRender.Controller();
        isInit = true;
        Calbunny();
    }

    void Calbunny()
    {
        GameObject bunny = GameObject.FindGameObjectWithTag("bunny");
        Mesh bunnymesh = bunny.GetComponent<MeshFilter>().mesh;
        int num = bunnymesh.vertexCount;
        Vector3[] ver = bunnymesh.vertices;
        Vector3[] nor = bunnymesh.normals;
        bunnyspace = new Vector3[60 * 100 * 40];
        bunnyspace.Initialize();
        for (int i = 0; i < num; i++)
        {
            float x = ver[i][0];
            float y = ver[i][1];
            float z = ver[i][2];
            Vector3 orientation = ver[i];
            orientation.Normalize();
            int len = (int)Vector3.Distance(ver[i], new Vector3(0, 0, 0));
            for (int p = 1; p < 10 * (len + 1.0f); p++)
            {
                Vector3 tmp = orientation * 0.1f * p;
                int floorx = (int)(5.0f * (tmp[0] + 6.0f));
                int floory = (int)(5.0f * (tmp[1] + 10.0f));
                int floorz = (int)(5.0f * (tmp[2] + 4.0f));

                if (floorx < 0 || floorx > 58 || floory < 0 || floory > 98 || floorz < 0 || floorz > 38)
                {
                    break;
                }
                bunnyspace[4000 * floorx + 40 * floory + floorz] = nor[i];
                bunnyspace[4000 * floorx + 40 * (floory + 1) + floorz] = nor[i];
                bunnyspace[4000 * floorx + 40 * floory + floorz + 1] = nor[i];
                bunnyspace[4000 * floorx + 40 * (floory + 1) + floorz + 1] = nor[i];
                bunnyspace[4000 * (floorx + 1) + 40 * floory + floorz] = nor[i];
                bunnyspace[4000 * (floorx + 1) + 40 * (floory + 1) + floorz] = nor[i];
                bunnyspace[4000 * (floorx + 1) + 40 * floory + floorz + 1] = nor[i];
                bunnyspace[4000 * (floorx + 1) + 40 * (floory + 1) + floorz + 1] = nor[i];


            }
        }
        bunnyspace_buffer = new ComputeBuffer(60 * 100 * 40, 12);
        bunnyspace_buffer.SetData(bunnyspace);
        collisionResponseCS.SetBuffer(CRCS_bunnyhandle, "Bunnyspace", bunnyspace_buffer);
    }

    void Init() {
        deltaT = Time.deltaTime / looper;
        InitClothData();
        InitComputeShader();
    }

    void InitClothData() {
        verCount = verCountPerCol * verCountPerRow;
        triIdxSize = (verCountPerCol - 1) * (verCountPerRow - 1) * 6;
        
        positions = new Vector4[verCount];
        texcoords = new Vector2[verCount];
        tris = new int[triIdxSize];
        triIdx = 0;

        positions.Initialize();
        texcoords.Initialize();
        tris.Initialize();

        leftFixP = Vector4.zero;
        leftFixP.w = 1.0f;
        rightFixP = new Vector4(gridSize * (verCountPerRow - 1), 0, 0, 1);
        Vector4 leftFixPWorld = clothObj.transform.TransformPoint(leftFixP);
        leftFixPWorld.w = 1.0f;
        Vector4 clothRight = clothObj.transform.right * gridSize;
        Vector4 clothForward = clothObj.transform.forward * gridSize;

        for (int row = 0; row < verCountPerCol; ++row) {
            for (int col = 0; col < verCountPerRow; ++col) {
                int idx = row * verCountPerRow + col;
                positions[idx] = leftFixPWorld + row * clothRight + col * clothForward;
                texcoords[idx] = 
                    new Vector2(col / (float)(verCountPerRow - 1), row / (float)(verCountPerCol - 1));
            }
        }
        
        // front face tris
        for (int row = 0; row < verCountPerCol - 1; ++row) {
            for (int col = 0; col < verCountPerRow - 1; ++col) {
                tris[triIdx] = row * verCountPerRow + col;
                tris[triIdx + 1] = (row + 1) * verCountPerRow + col + 1;
                tris[triIdx + 2] = row * verCountPerRow + col + 1;
                tris[triIdx + 3] = row * verCountPerRow + col;
                tris[triIdx + 4] = (row + 1) * verCountPerRow + col;
                tris[triIdx + 5] = (row + 1) * verCountPerRow + col + 1;

                triIdx += 6;
            }
        }
        
        // init normal
        normals = new Vector4[verCount];
        normals.Initialize();
        // init tangent
        tangents = new Vector2[verCount];
        tangents.Initialize();
        // init acceleration
        accelerations = new Vector4[verCount];
        for (int i = 0; i < verCount; ++i) {
            accelerations[i] = g;       // vec3->vec4的隐式转换效果未知
        }
        // init link info
        linkInfos = new uint[verCount];
        for (int i = 0; i < verCount; ++i) {
            linkInfos[i] = (uint)0x3F;
        }
    }

    void InitComputeShader() {
        InitComputeBuffer();
        InitNUCS();
        InitCRCS();
        InitNCS();
        InitTCS();
        InitACS();
    }

    void InitComputeBuffer() {
        TRS_buffer = new ComputeBuffer(8, 16);
        // position
        p1_bufferPool = new ComputeBuffer[2];
        p0_buffer = new ComputeBuffer(verCount, 16);
        p0_buffer.SetData(positions);
        p1_bufferPool[0] = new ComputeBuffer(verCount, 16);
        p1_bufferPool[0].SetData(positions);
        p1_bufferPool[1] = new ComputeBuffer(verCount, 16);
        p1_bufferPool[1].SetData(positions);
        // texcoord
        texcoord_buffer = new ComputeBuffer(verCount, 8);
        texcoord_buffer.SetData(texcoords);
        // tris buffer
        tris_buffer = new ComputeBuffer(triIdxSize, 4);
        tris_buffer.SetData(tris);
        // normal buffer
        normal_buffer = new ComputeBuffer(verCount, 16);
        normal_buffer.SetData(normals);
        // tangent buffer
        tangent_buffer = new ComputeBuffer(verCount, 16);
        tangent_buffer.SetData(tangents);
        // acceleration buffer
        acceleration_buffer = new ComputeBuffer(verCount, 16);
        acceleration_buffer.SetData(accelerations);
        // linkInfo buffer
        linkInfo_buffer = new ComputeBuffer(verCount, 4);
        linkInfo_buffer.SetData(linkInfos);
        // anchor related buffer
        anchorRowAndCol_buffer = new ComputeBuffer(20, 8);
        anchorRowAndCol_buffer.SetData(anchorRowAndCol);
        anchorRelPos_buffer = new ComputeBuffer(20, 16);
        anchorRelPos_buffer.SetData(anchorRelPos);
    }

    void InitNUCS() {
        NUCS_handle = nodeUpdateCS.FindKernel("NodeUpdate");
        NUCS_lengthHandle = nodeUpdateCS.FindKernel("NodeCorrection_length");
        NUCS_bendingHandle = nodeUpdateCS.FindKernel("NodeCorrection_bending");
        NUCS_linkCorrectionHandle = nodeUpdateCS.FindKernel("NodeLinkCorrection");
        NUCS_linkCorrectionInvHandle = nodeUpdateCS.FindKernel("NodeLinkCorrection_INV");

        nodeUpdateCS.SetInt("_VerCountPerCol", verCountPerCol);
        nodeUpdateCS.SetInt("_VerCountPerRow", verCountPerRow);
        nodeUpdateCS.SetInt("_IterCount", iterCount);
        nodeUpdateCS.SetInt("_AnchorCount", anchorCount);
        nodeUpdateCS.SetFloat("_GridSize", gridSize);
        nodeUpdateCS.SetFloat("_BendingAngle", bendingAngle);
        nodeUpdateCS.SetFloat("_Damping", damping);
        nodeUpdateCS.SetFloat("_DeltaT", deltaT);
        nodeUpdateCS.SetFloat("_DeltaT2", deltaT * deltaT);
        nodeUpdateCS.SetFloat("lineBreakThres", lineBreakThres);
        nodeUpdateCS.SetVector("_G", g);
        nodeUpdateCS.SetVector("_LeftFixP", leftFixP);
        nodeUpdateCS.SetVector("_RightFixP", rightFixP);

        // set buffer
        nodeUpdateCS.SetBuffer(NUCS_handle, "_P0_buffer", p0_buffer);
        nodeUpdateCS.SetBuffer(NUCS_handle, "_P1_buffer", p1_bufferPool[0]);
        nodeUpdateCS.SetBuffer(NUCS_handle, "_Acceleration", acceleration_buffer);
        
        nodeUpdateCS.SetBuffer(NUCS_lengthHandle, "_P1_buffer", p1_bufferPool[0]);
        nodeUpdateCS.SetBuffer(NUCS_lengthHandle, "_P1_bufferTmp", p1_bufferPool[1]);
        nodeUpdateCS.SetBuffer(NUCS_lengthHandle, "_LinkInfo", linkInfo_buffer);

        nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_P1_buffer", p1_bufferPool[1]);
        nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_P1_bufferTmp", p1_bufferPool[0]);
        nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_LinkInfo", linkInfo_buffer);
        nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_AnchorRowCol", anchorRowAndCol_buffer);
        nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_AnchorRelPos", anchorRelPos_buffer);

        nodeUpdateCS.SetBuffer(NUCS_linkCorrectionHandle, "_P1_buffer", p1_bufferPool[0]);
        nodeUpdateCS.SetBuffer(NUCS_linkCorrectionHandle, "_LinkInfo", linkInfo_buffer);
        nodeUpdateCS.SetBuffer(NUCS_linkCorrectionHandle, "_Trimap", tris_buffer);

        nodeUpdateCS.SetBuffer(NUCS_linkCorrectionInvHandle, "_LinkInfo", linkInfo_buffer);

        // set dispatchdim
        NUCS_dispatchDim.x = verCountPerCol / 8;    // x代表行号
        NUCS_dispatchDim.y = verCountPerRow / 8;    // y代表列号
        NUCS_dispatchDim.z = 1;
    }

    void InitCRCS() {
        CRCS_spherehandle = collisionResponseCS.FindKernel("SphereCollisionResponse");
        CRCS_cubehandle = collisionResponseCS.FindKernel("CubeCollisionResponse");
        CRCS_innerhandle = collisionResponseCS.FindKernel("InnerCollisionResponse");
        CRCS_swaphandle = collisionResponseCS.FindKernel("SwapCollisionResponse");
        CRCS_bunnyhandle = collisionResponseCS.FindKernel("BunnyCollisionResponse");

        collisionResponseCS.SetInt("_VerCountPerCol", verCountPerCol);
        collisionResponseCS.SetInt("_VerCountPerRow", verCountPerRow);
        collisionResponseCS.SetFloat("_DeltaT", deltaT);

        // set dispatchdim
        CRCS_dispatchDim.x = verCountPerCol / 8;
        CRCS_dispatchDim.y = verCountPerRow / 8;
        CRCS_dispatchDim.z = 1;
    }

    void InitNCS() {
        NCS_handle = normalCS.FindKernel("Norm");

        normalCS.SetInt("_VerCountPerCol", verCountPerCol);
        normalCS.SetInt("_VerCountPerRow", verCountPerRow);

        normalCS.SetBuffer(NCS_handle, "_Position", p1_bufferPool[0]);  // 世界坐标
        normalCS.SetBuffer(NCS_handle, "_Normal", normal_buffer);
        normalCS.SetBuffer(NCS_handle, "_LinkInfo", linkInfo_buffer);
    }

    void InitTCS() {
        TCS_handle = tangentCS.FindKernel("TangentCompute");

        tangentCS.SetInt("_VerCountPerCol", verCountPerCol);
        tangentCS.SetInt("_VerCountPerRow", verCountPerRow);

        tangentCS.SetBuffer(TCS_handle, "_Position", p1_bufferPool[0]);
        tangentCS.SetBuffer(TCS_handle, "_Normal", normal_buffer);
        tangentCS.SetBuffer(TCS_handle, "_UVs", texcoord_buffer);
        tangentCS.SetBuffer(TCS_handle, "_Tangent", tangent_buffer);
        tangentCS.SetBuffer(TCS_handle, "_LinkInfo", linkInfo_buffer);
    }

    void InitACS() {
        ACS_handle = accelerationCS.FindKernel("WindForceCompute");

        accelerationCS.SetInt("_VerCountPerCol", verCountPerCol);
        accelerationCS.SetInt("_VerCountPerRow", verCountPerRow);
        accelerationCS.SetFloat("_DeltaT_INV", 1.0f / deltaT);
        accelerationCS.SetFloat("_Drag", dragCoeff);
        accelerationCS.SetFloat("_Lift", liftCoeff);
        accelerationCS.SetVector("_Wind_V", wind_V);
        accelerationCS.SetVector("_G", g);

        accelerationCS.SetFloat("_WindNoiseScale", windNoiseScale);
        accelerationCS.SetFloat("_WindNoiseGridSize", windNoiceGridSize);
        accelerationCS.SetFloat("_WindNoiseStrength", windNoiceStrength);
        accelerationCS.SetFloat("_WindVibrationStrength", windVibrationStrength);

        accelerationCS.SetBuffer(ACS_handle, "_P0_buffer", p0_buffer);
        accelerationCS.SetBuffer(ACS_handle, "_P1_buffer", p1_bufferPool[0]);
        accelerationCS.SetBuffer(ACS_handle, "_Normal", normal_buffer);
        accelerationCS.SetBuffer(ACS_handle, "_Acceleration", acceleration_buffer);

        accelerationCS.SetFloat("_Time", 2 * Time.time);

        UpdateWindTRS();
    }

    Matrix4x4 GetModelMatrix() {
        Matrix4x4 ret = clothObj.transform.localToWorldMatrix;
        Transform curTrans = clothObj.transform;
        while (curTrans.parent) {
            ret = curTrans.parent.localToWorldMatrix * ret;
            curTrans = curTrans.parent;
        }
        return ret;
    }

    void OnDestroy() {
        p0_buffer.Release();
        p1_bufferPool[0].Release();
        p1_bufferPool[1].Release();
        TRS_buffer.Release();
        texcoord_buffer.Release();
        tris_buffer.Release();
        normal_buffer.Release();
        tangent_buffer.Release();
        acceleration_buffer.Release();
        linkInfo_buffer.Release();
        anchorRowAndCol_buffer.Release();
        anchorRelPos_buffer.Release();
    }

    Vector4[] Mat4x4ToFloatArray(Matrix4x4 input) {
        Vector4[] ret = new Vector4[4];
        ret[0] = new Vector4(input.m00, input.m01, input.m02, input.m03);
        ret[1] = new Vector4(input.m10, input.m11, input.m12, input.m13);
        ret[2] = new Vector4(input.m20, input.m21, input.m22, input.m23);
        ret[3] = new Vector4(input.m30, input.m31, input.m32, input.m33);
        return ret;
    }

    void UpdateClothTrs() {
        clothTRS[0] = GetModelMatrix();
        clothTRS[1] = clothTRS[0].inverse;
        Vector4[] tmp = new Vector4[8];
        Mat4x4ToFloatArray(clothTRS[0]).CopyTo(tmp, 0);
        Mat4x4ToFloatArray(clothTRS[1]).CopyTo(tmp, 4);
        TRS_buffer.SetData(tmp);
    }

    void UpdateWindTRS() {
        // get min ele in wind_V
        if (wind_V.x <= wind_V.y && wind_V.x <= wind_V.z) {
            Vector3 windLocalX = new Vector3(0, -wind_V.z, wind_V.y).normalized;
            Vector3 windLocalY = Vector3.Cross(wind_V, windLocalX).normalized;
            accelerationCS.SetVector("_Wind_WorldToLocal0", windLocalX);
            accelerationCS.SetVector("_Wind_WorldToLocal1", windLocalY);
        }
        else if (wind_V.y <= wind_V.x && wind_V.y <= wind_V.z) {
            Vector3 windLocalX = new Vector3(-wind_V.z, 0, wind_V.x).normalized;
            Vector3 windLocalY = Vector3.Cross(wind_V, windLocalX).normalized;
            accelerationCS.SetVector("_Wind_WorldToLocal0", windLocalX);
            accelerationCS.SetVector("_Wind_WorldToLocal1", windLocalY);
        }
        else {
            Vector3 windLocalX = new Vector3(-wind_V.y, wind_V.x, 0).normalized;
            Vector3 windLocalY = Vector3.Cross(wind_V, windLocalX).normalized;
            accelerationCS.SetVector("_Wind_WorldToLocal0", windLocalX);
            accelerationCS.SetVector("_Wind_WorldToLocal1", windLocalY);
        }
    }

    void UpdatePhysicsAttri() {
        // Cloth Attributes
        nodeUpdateCS.SetInt("_AnchorCount", anchorCount);
        anchorRowAndCol_buffer.SetData(anchorRowAndCol);
        anchorRelPos_buffer.SetData(anchorRelPos);

        // Wind Attributes
        accelerationCS.SetFloat("_Drag", dragCoeff);
        accelerationCS.SetFloat("_Lift", liftCoeff);
        accelerationCS.SetVector("_Wind_V", wind_V);
        accelerationCS.SetVector("_G", g);

        accelerationCS.SetFloat("_WindNoiseScale", windNoiseScale);
        accelerationCS.SetFloat("_WindNoiseGridSize", windNoiceGridSize);
        accelerationCS.SetFloat("_WindNoiseStrength", windNoiceStrength);
        accelerationCS.SetFloat("_WindVibrationStrength", windVibrationStrength);

        accelerationCS.SetFloat("_Time", 2 * Time.time);

        UpdateWindTRS();
    }

    void FixedUpdate() {
        if (!isInit) {
            return;
        }
        UpdateClothTrs();
        UpdatePhysicsAttri();
        nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_TRS_Mat", TRS_buffer);

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying) {
            return;
        }
#endif
        for (int i = 0; i < looper; ++i) {
            // verlet积分更新位置
            nodeUpdateCS.SetBuffer(NUCS_handle, "_P0_buffer", p0_buffer);
            nodeUpdateCS.SetBuffer(NUCS_handle, "_P1_buffer", p1_bufferPool[0]);
            nodeUpdateCS.Dispatch(
                NUCS_handle, 
                NUCS_dispatchDim.x, 
                NUCS_dispatchDim.y,
                NUCS_dispatchDim.z
            );

            for (int iter = 0; iter < iterCount; ++iter) {
                // 距离约束
                nodeUpdateCS.SetBuffer(NUCS_lengthHandle, "_P1_buffer", p1_bufferPool[0]);
                nodeUpdateCS.SetBuffer(NUCS_lengthHandle, "_P1_bufferTmp", p1_bufferPool[1]);
                nodeUpdateCS.Dispatch(
                    NUCS_lengthHandle,
                    NUCS_dispatchDim.x, 
                    NUCS_dispatchDim.y,
                    NUCS_dispatchDim.z
                );
                // 弯曲约束
                nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_P1_buffer", p1_bufferPool[1]);
                nodeUpdateCS.SetBuffer(NUCS_bendingHandle, "_P1_bufferTmp", p1_bufferPool[0]);
                nodeUpdateCS.Dispatch(
                    NUCS_bendingHandle,
                    NUCS_dispatchDim.x, 
                    NUCS_dispatchDim.y,
                    NUCS_dispatchDim.z
                );
                collisionResponseCS.SetBuffer(CRCS_spherehandle, "_P1_buffer", p1_bufferPool[0]);
                collisionResponseCS.SetBuffer(CRCS_cubehandle, "_P1_buffer", p1_bufferPool[0]);
                collisionResponseCS.SetBuffer(CRCS_bunnyhandle, "_P1_buffer", p1_bufferPool[0]);
                GameObject[] spheres;
                GameObject[] cubes;
                GameObject[] bunnyedges;
                GameObject[] bunnys;
                spheres = GameObject.FindGameObjectsWithTag(spheretag);
                cubes = GameObject.FindGameObjectsWithTag(cubetag);
                bunnyedges = GameObject.FindGameObjectsWithTag("bunnyedge");
                bunnys = GameObject.FindGameObjectsWithTag("bunny");

                //handle spheres
                foreach (GameObject sphere in spheres)
                {

                    Vector3 pos = sphere.transform.position;
                    float radius = sphere.transform.localScale[0] * 0.5f;
                    collisionResponseCS.SetFloat("Radius", 1.3f*radius);
                    collisionResponseCS.SetVector("SphereCenter", new Vector4(pos[0] , pos[1] , pos[2] , 1));
                    collisionResponseCS.Dispatch(
                        CRCS_spherehandle,
                        CRCS_dispatchDim.x,
                        CRCS_dispatchDim.y,
                        CRCS_dispatchDim.z
                    );
                }

                //handle cubes
                foreach (GameObject cube in cubes)
                {
                    Vector3 pos = cube.transform.position;
                    Vector3 scale = cube.transform.localScale * 0.5f;
                    cube.transform.localScale = new Vector3(1, 1, 1);
                    Matrix4x4 rot = cube.transform.worldToLocalMatrix;
                    cube.transform.localScale = scale * 2;
                    Matrix4x4 rot_inv = rot.inverse;

                    collisionResponseCS.SetVector("CubeCenter", new Vector4(pos[0], pos[1], pos[2], 1));
                    collisionResponseCS.SetVector("CubeScale", new Vector4(scale[0]*1.1f, scale[1] * 1.1f, scale[2] * 1.1f, 0));
                    collisionResponseCS.SetMatrix("Cuberot", rot);
                    collisionResponseCS.SetMatrix("Cuberot_inv", rot_inv);

                    collisionResponseCS.Dispatch(
                        CRCS_cubehandle,
                        CRCS_dispatchDim.x,
                        CRCS_dispatchDim.y,
                        CRCS_dispatchDim.z
                    );
                }

                //handle buuny 碰撞球
                //foreach (GameObject bunnyedge in bunnyedges)
                //{
                //    Vector3 bunnycenter = bunnyedge.transform.parent.position;
                //    Vector3 bunnyedgepos = bunnyedge.transform.position;
                //    collisionResponseCS.SetVector("Bunnycenter", new Vector4(bunnycenter[0], bunnycenter[1], bunnycenter[2], 1));
                //    collisionResponseCS.SetVector("Bunnyedge", new Vector4(bunnyedgepos[0], bunnyedgepos[1], bunnyedgepos[2], 1));

                //    collisionResponseCS.Dispatch(
                //        CRCS_bunnyhandle,
                //        CRCS_dispatchDim.x / 8,
                //        CRCS_dispatchDim.y / 8,
                //        CRCS_dispatchDim.z
                //    );
                //}

                foreach (GameObject bunny in bunnys)
                {
                    Matrix4x4 rot = bunny.transform.localToWorldMatrix;
                    Matrix4x4 rot_inv = rot.inverse;
                    collisionResponseCS.SetMatrix("Bunnyrot", rot);
                    collisionResponseCS.SetMatrix("Bunnyrot_inv", rot_inv);
                    collisionResponseCS.Dispatch(
                        CRCS_bunnyhandle,
                        CRCS_dispatchDim.x,
                        CRCS_dispatchDim.y,
                        CRCS_dispatchDim.z
                    );
                }

                //内部碰撞
                collisionResponseCS.SetBuffer(CRCS_innerhandle, "_P1_buffer", p1_bufferPool[0]);
                collisionResponseCS.SetBuffer(CRCS_innerhandle, "_P1_bufferTmp", p1_bufferPool[1]);
                collisionResponseCS.Dispatch(
                        CRCS_innerhandle,
                        CRCS_dispatchDim.x / 8,
                        CRCS_dispatchDim.y / 8,
                        CRCS_dispatchDim.z
                );

                collisionResponseCS.SetBuffer(CRCS_swaphandle, "_P1_buffer", p1_bufferPool[0]);
                collisionResponseCS.SetBuffer(CRCS_swaphandle, "_P1_bufferTmp", p1_bufferPool[1]);
                collisionResponseCS.Dispatch(
                        CRCS_swaphandle,
                        CRCS_dispatchDim.x,
                        CRCS_dispatchDim.y,
                        CRCS_dispatchDim.z
                );

                // p1_bufferPoolSwapIdx = (p1_bufferPoolSwapIdx + 1) % 2;
            }
        }
        // 处理布料边的断裂
        nodeUpdateCS.Dispatch(NUCS_linkCorrectionHandle, verCountPerCol / 8, verCountPerRow / 8, 1);
        nodeUpdateCS.Dispatch(NUCS_linkCorrectionInvHandle, verCountPerCol / 8, verCountPerRow / 8, 1);

        normalCS.Dispatch(NCS_handle, verCountPerCol / 8, verCountPerRow / 8, 1);
        tangentCS.Dispatch(TCS_handle, verCountPerCol / 8, verCountPerRow / 8, 1);
        accelerationCS.Dispatch(ACS_handle, verCountPerCol / 8, verCountPerRow / 8, 1);
        collisionResponseCS.SetBuffer(CRCS_spherehandle, "_P1_buffer", p1_bufferPool[0]);
        collisionResponseCS.SetBuffer(CRCS_cubehandle, "_P1_buffer", p1_bufferPool[0]);
        collisionResponseCS.SetBuffer(CRCS_bunnyhandle, "_P1_buffer", p1_bufferPool[0]);
        collisionResponseCS.SetBuffer(CRCS_spherehandle, "_TRS_Mat", TRS_buffer);
        collisionResponseCS.SetBuffer(CRCS_cubehandle, "_TRS_Mat", TRS_buffer);
        collisionResponseCS.SetBuffer(CRCS_bunnyhandle, "_TRS_Mat", TRS_buffer);
        
    }
}
