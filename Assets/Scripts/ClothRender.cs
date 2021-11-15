// #define CUBE_SURSHADER
#define BRDF_SHADER
#define BRDF_BUMP_SHADER


using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ClothRender : MonoBehaviour {

    public Material material;
    [Header("Defualt Shader")]
    public MeshTopology meshTopology;
    [Header("Cube Surface Shader")]
    public Mesh mesh;
    [Header("BRDF Shader")]
    [Range(0.0f, 1.0f)] public float alpha = 0.5f;
    [Range(0.0f, 1.0f)] public float K_amp = 0.5f;
    [Range(0.0f, 1.0f)] public float metallicness = 0.0f;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer texcoordBuffer;
    private ComputeBuffer trisBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer tangentBuffer;
    
    private int verCountPerCol, verCountPerRow, verCount;

    private ClothSimulation anchor;

    [HideInInspector] public bool ready = false;


    public void Controller() {
        anchor = GetComponent<ClothSimulation>();
        Linker();
        InitMaterial();
        ready = true;
    }

    void Linker() {
        positionBuffer = anchor.p1_bufferPool[0];
        texcoordBuffer = anchor.texcoord_buffer;
        trisBuffer = anchor.tris_buffer;
        normalBuffer = anchor.normal_buffer;
        tangentBuffer = anchor.tangent_buffer;

        verCount = anchor.verCount;
        verCountPerCol = anchor.verCountPerCol;
        verCountPerRow = anchor.verCountPerRow;
    }

    void InitMaterial() {
#if CUBE_SURSHADER
        material.SetBuffer("_Positions", positionBuffer);
        material.SetFloat("_Step", 1.0f / (verCountPerCol - 1));
#elif BRDF_SHADER
        material.SetBuffer("Position", positionBuffer);
        material.SetBuffer("Trimap", trisBuffer);
        material.SetBuffer("Texcoord", texcoordBuffer);
        material.SetBuffer("Normal", normalBuffer);
    #if BRDF_BUMP_SHADER
        material.SetBuffer("Tangent", tangentBuffer);
    #endif

        material.SetInt("verCountPerCol", verCountPerCol);
        material.SetInt("verCountPerRow", verCountPerRow); 

        material.SetFloat("_Alpha", alpha);
        material.SetFloat("_K_amp", K_amp);
        material.SetFloat("_Metallicness", metallicness);
#else
        material.SetBuffer("Position", positionBuffer);
        material.SetBuffer("Trimap", trisBuffer);
        material.SetBuffer("Texcoord", texcoordBuffer);
        material.SetBuffer("Normal", normalBuffer);

        material.SetInt("verCountPerCol", verCountPerCol);
        material.SetInt("verCountPerRow", verCountPerRow);  
#endif
    }

    void UpdateMaterial() {
#if BRDF_SHADER
    material.SetFloat("_Alpha", alpha);
    material.SetFloat("_K_amp", K_amp);
    material.SetFloat("_Metallicness", metallicness);
#endif
    }

    void Update() {
#if CUBE_SURSHADER
        if (!ready) {
            return;
        }
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 13);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, verCount);
#endif
        UpdateMaterial();
    }

    void OnRenderObject() {
#if CUBE_SURSHADER
#else    
        if (!ready) {
            return;
        }
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, (verCountPerCol - 1) * (verCountPerRow - 1) * 6, 1);
        material.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, (verCountPerCol - 1) * (verCountPerRow - 1) * 6, 1);
        
        // switch (meshTopology) {
        //     case MeshTopology.Points:
        //         Graphics.DrawProceduralNow(meshTopology, verCount, 1);
        //         break;
        //     case MeshTopology.Triangles:
        //         Graphics.DrawProceduralNow(meshTopology, (verCountPerCol - 1) * (verCountPerRow - 1) * 6, 1);
        //         break;
        //     case MeshTopology.LineStrip:
        //         Graphics.DrawProceduralNow(meshTopology, (verCountPerCol - 1) * (verCountPerRow - 1) * 6, 1);
        //         break;
        //     case MeshTopology.Quads:
        //         Graphics.DrawProceduralNow(meshTopology, verCount / 4, 1);
        //         break;
        //     case MeshTopology.Lines:
        //         break;
        //     default:
        //         Debug.Log("Unhandled Mesh Topology");
        //         break;
        // }
 #endif  
    }
}

