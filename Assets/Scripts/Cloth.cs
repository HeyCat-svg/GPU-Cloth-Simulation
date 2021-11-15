using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloth : MonoBehaviour {

    public GameObject clothObj;
    public int verCountPerRow = 30;
    public int verCountPerCol =  30;
    public float gridSize = 0.2f;
    public Material clothMaterial;

    struct Node {
        public Vector3 p0, p1;
    }

    private Node[] nodes;
    private float[][] Q;
    private Vector3 leftFixP, rightFixP;    // 相对位置
    private int debugCount = 0;
    
    void Start() {
        InitCloth();
    }
    
    void Update() {
        DrawCloth();
    }

    void FixedUpdate() {
        UpdateClothState();
    }

    void InitCloth() {
        nodes = new Node[verCountPerRow * verCountPerCol];
        
        leftFixP = Vector3.zero;
        rightFixP = new Vector3(gridSize * (verCountPerRow - 1), 0, 0);
        Vector3 leftFixPWorld = clothObj.transform.TransformPoint(leftFixP);
        Vector3 clothRight = clothObj.transform.right * gridSize;
        Vector3 clothForward = clothObj.transform.forward * gridSize;

        for (int row = 0; row < verCountPerCol; ++row) {
            for (int col = 0; col < verCountPerRow; ++col) {
                Node node = new Node();
                node.p0 = leftFixPWorld + row * clothRight + col * clothForward;
                node.p1 = node.p0;

                nodes[row * verCountPerRow + col] = node;
            }
        }

        // 计算Q
        // float A0, A1;
        // A0 = A1 = gridSize * gridSize * 0.5f;
        // float fac = 3.0f / (A0 + A1);     // mutilply factor
        Q = new float[4][];
        for (int i = 0; i < 4; ++i) {
            Q[i] = new float[4];
            for (int j = 0; j < 4; ++j) {
                Q[i][j] = 0.0f;
            }
        }
        // Q[2][2] = 4 * fac;
        // Q[2][3] = -4 * fac;
        // Q[3][2] = -4 * fac;
        // Q[3][3] = 4 * fac;
    }

    Vector3 Verlet(Vector3 p0, Vector3 p1, float damping, Vector3 a, float dt) {
        Vector3 result = p1 + damping * (p1 - p0) + a * dt * dt;
        return result;
    }

    Vector3[] LengthConstraint(Vector3 p1, Vector3 p2, float length) {
        Vector3 deltaP = p2 - p1;
        float m = deltaP.magnitude;
        Vector3 offset = deltaP * (m - length) / (2 * m);

        Vector3[] ret = new Vector3[2];
        ret[0] = p1 + offset;
        ret[1] = p2 - offset;

        return ret;
    }

    Vector3[] IosmetricBendingConstraint(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4) {
        Vector3[] x = new Vector3[4];
        x[0] = p1;
        x[1] = p2;
        x[2] = p3;
        x[3] = p4;

        // calculate Q matrix
        Vector3 x10 = x[1] - x[0];
        Vector3 x20 = x[2] - x[0];
        Vector3 x30 = x[3] - x[0];

        Vector3 e0 = x10.normalized;
        Vector3 e1 = (x[2] - x[1]).normalized;
        Vector3 e2 = (-x20).normalized;
        Vector3 e3 = x30.normalized;
        Vector3 e4 = (x[1] - x[3]).normalized;

        float c01 = Cot(e0, e1);
        float c04 = Cot(e0, e4);
        float c02 = Cot(e0, e2);
        float c03 = Cot(e0, e3);

        Vector4 K = Vector4.zero;
        K[0] = c01 + c04;
        K[1] = c02 + c03;
        K[2] = -c01 - c02;
        K[3] = -c03 - c04;

        float A0 = GetS(x20, x10);
        float A1 = GetS(x10, x30);
        float fac = 3.0f / (A0 + A1);

        for (int i = 0; i < 4; ++i) {
            for (int j = 0; j < 4; ++j) {
                Q[i][j] = fac * K[i] * K[j];
            }
        }
        
        Vector3[] gradient = new Vector3[4];

        // 计算梯度
        for (int i = 0; i < 4; ++i) {
            Vector3 s = Vector3.zero;
            for (int j = 0; j < 4; ++j) {
                s += Q[i][j] * x[j];
            }
            gradient[i] = s;
        }

        // 计算C(x)
        float cx = 0.0f;
        for (int i = 0; i < 4; ++i) {
            for (int j = 0; j < 4; ++j) {
                cx += Q[i][j] * Vector3.Dot(x[i], x[j]);
            }
        }
        cx *= 0.5f;

        // 计算分母与lambda
        float d = 0.0f;
        for (int i = 0; i < 4; ++i) {
            d += Mathf.Pow(gradient[i].magnitude, 2);
        }
        float lambda = cx / d;

        // 计算结果
        Vector3[] ret = new Vector3[4];
        for (int i = 0; i < 4; ++i) {
            ret[i] = -lambda * gradient[i];
        }

        return ret;
    }

    /* 返回的是变化量 */
    Vector3[] NormalBending(Vector3 _p1, Vector3 _p2, Vector3 _p3, Vector3 _p4) {
        Vector3 p2 = _p2 - _p1;
        Vector3 p3 = _p3 - _p1;
        Vector3 p4 = _p4 - _p1;

        Vector3 n1 = Vector3.Cross(p2, p3).normalized;
        Vector3 n2 = Vector3.Cross(p2, p4).normalized;

        float d = Vector3.Dot(n1, n2);

        Vector3[] q = new Vector3[4];

        q[2] = (Vector3.Cross(p2, n2) + Vector3.Cross(n1, p2) * d) / Mathf.Max(Vector3.Cross(p2, p3).magnitude, 0.0001f);
        q[3] = (Vector3.Cross(p2, n1) + Vector3.Cross(n2, p2) * d) / Mathf.Max(Vector3.Cross(p2, p4).magnitude, 0.0001f);
        q[1] = -(Vector3.Cross(p3, n2) + Vector3.Cross(n1, p3) * d) / Mathf.Max(Vector3.Cross(p2, p3).magnitude, 0.0001f)
                - (Vector3.Cross(p4, n1) + Vector3.Cross(n2, p4) * d) / Mathf.Max(Vector3.Cross(p2, p4).magnitude, 0.0001f);
        q[0] = -(q[1] + q[2] + q[3]);

        // 分母
        float div = 0.0f;
        for (int i = 0; i < 4; ++i) {
            div += q[i].sqrMagnitude;
        }

        // 因子
        float fact = -Mathf.Sqrt(Mathf.Max(1.0f - d * d, 0)) * (Mathf.Acos(Mathf.Clamp(d, -1, 1)) - Mathf.PI) / Mathf.Max(div, 0.0001f);

        Vector3[] ret = new Vector3[4];
        for (int i = 0; i < 4; ++i) {
            ret[i] = fact * q[i];
        }

        // Debug.Log("" + ret[0] + ret[1] + ret[2] + ret[3]);

        return ret;
    }

    void UpdateClothState() {
        Vector3 a = new Vector3(0, -9.8f, 0);

        int nodeSize = nodes.Length;
        for (int i = 0; i < nodeSize; ++i) {
            Node n = nodes[i];
            Vector3 p2 = Verlet(n.p0, n.p1, 1.0f, a, Time.deltaTime);
            n.p0 = n.p1;
            n.p1 = p2;
            nodes[i] = n;
        }

        for (int iter = 0; iter < 3; ++iter) {
            for (int row = 0; row < verCountPerCol - 1; ++row) {
                for (int col = 0; col < verCountPerRow - 1; ++col) {
                    // length constraint
                    Vector3[] ret = LengthConstraint(
                        nodes[row * verCountPerRow + col].p1,
                        nodes[row * verCountPerRow + col + 1].p1,
                        gridSize
                    );
                    nodes[row * verCountPerRow + col].p1 = ret[0];
                    nodes[row * verCountPerRow + col + 1].p1 = ret[1];

                    ret = LengthConstraint(
                        nodes[row * verCountPerRow + col].p1,
                        nodes[(row + 1) * verCountPerRow + col].p1,
                        gridSize
                    );
                    nodes[row * verCountPerRow + col].p1 = ret[0];
                    nodes[(row + 1) * verCountPerRow + col].p1 = ret[1];

                    ret = LengthConstraint(
                        nodes[row * verCountPerRow + col].p1,
                        nodes[(row + 1) * verCountPerRow + col + 1].p1,
                        gridSize * Mathf.Sqrt(2)
                    );
                    nodes[row * verCountPerRow + col].p1 = ret[0];
                    nodes[(row + 1) * verCountPerRow + col + 1].p1 = ret[1];

                    if (col == verCountPerRow - 2) {
                        ret = LengthConstraint(
                            nodes[row * verCountPerRow + col + 1].p1,
                            nodes[(row + 1) * verCountPerRow + col + 1].p1,
                            gridSize
                        );
                        nodes[row * verCountPerRow + col + 1].p1 = ret[0];
                        nodes[(row + 1) * verCountPerRow + col + 1].p1 = ret[1];
                    }
                    if (row == verCountPerCol - 2) {
                        ret = LengthConstraint(
                            nodes[(row + 1) * verCountPerRow + col].p1,
                            nodes[(row + 1) * verCountPerRow + col + 1].p1,
                            gridSize
                        );
                        nodes[(row + 1) * verCountPerRow + col].p1 = ret[0];
                        nodes[(row + 1) * verCountPerRow + col + 1].p1 = ret[1];
                    }

                    // // isometric bending
                    // ret = IosmetricBendingConstraint(
                    //     nodes[(row + 1) * verCountPerRow + col + 1].p1,
                    //     nodes[row * verCountPerRow + col].p1,
                    //     nodes[(row + 1) * verCountPerRow + col].p1,
                    //     nodes[row * verCountPerRow + col + 1].p1
                    // );
                    // nodes[(row + 1) * verCountPerRow + col + 1].p1 += ret[0];
                    // nodes[row * verCountPerRow + col].p1 += ret[1];
                    // nodes[(row + 1) * verCountPerRow + col].p1 += ret[2];
                    // nodes[row * verCountPerRow + col + 1].p1 += ret[3];
                
                    // if (debugCount % 20 == 0) {
                        // normal bending
                        if (col < verCountPerRow - 2 && row < verCountPerCol - 1) {
                            ret = NormalBending(
                                nodes[(row + 1) * verCountPerRow + col + 1].p1,
                                nodes[row * verCountPerRow + col + 1].p1,
                                nodes[row * verCountPerRow + col].p1,
                                nodes[(row + 1) * verCountPerRow + col + 2].p1
                            );
                            nodes[(row + 1) * verCountPerRow + col + 1].p1 += ret[0];
                            nodes[row * verCountPerRow + col + 1].p1 += ret[1];
                            nodes[row * verCountPerRow + col].p1 += ret[2];
                            nodes[(row + 1) * verCountPerRow + col + 2].p1 += ret[3];
                        }
                        if (col < verCountPerRow - 1 && row < verCountPerCol - 2) {
                            ret = NormalBending(
                                nodes[(row + 1) * verCountPerRow + col].p1,
                                nodes[(row + 1) * verCountPerRow + col + 1].p1,
                                nodes[row * verCountPerRow + col].p1,
                                nodes[(row + 2) * verCountPerRow + col + 1].p1
                            );
                            nodes[(row + 1) * verCountPerRow + col].p1 += ret[0];
                            nodes[(row + 1) * verCountPerRow + col + 1].p1 += ret[1];
                            nodes[row * verCountPerRow + col].p1 += ret[2];
                            nodes[(row + 2) * verCountPerRow + col + 1].p1 += ret[3];
                        }    
                    // }
                    // debugCount++;
                }
            }

            // 固定fixP
            nodes[0].p1 = clothObj.transform.TransformPoint(leftFixP);
            nodes[(verCountPerCol - 1) * verCountPerRow].p1 = clothObj.transform.TransformPoint(rightFixP);
        }
    }

    void DrawCloth() {
        int verCount = verCountPerRow * verCountPerCol;
        Vector3[] verts = new Vector3[verCount];
        Vector2[] uvs = new Vector2[verCount];
        List<int> tris = new List<int>();

        for (int row = 0; row < verCountPerCol; ++row) {
            for (int col = 0; col < verCountPerRow; ++col) {
                int curIdx = row * verCountPerRow + col;
                verts[curIdx] = nodes[curIdx].p1;
                uvs[curIdx] = new Vector2(0.5f, 0.5f);

                if (row == 0 || col == 0) {
                    continue;
                }

                tris.Add(curIdx);   // down-right
                tris.Add((row - 1) * verCountPerRow + col); // up-right
                tris.Add((row - 1) * verCountPerRow + col - 1); // up-left
                tris.Add((row - 1) * verCountPerRow + col - 1);
                tris.Add(row * verCountPerRow + col - 1);
                tris.Add(curIdx);
            }
        }

        Mesh m = new Mesh();
        m.vertices = verts;
        m.triangles = tris.ToArray();
        m.uv = uvs;
        m.RecalculateNormals();

        // draw cloth mesh
        Bounds bounds = new Bounds(clothObj.transform.position, 10.0f * Vector3.one);
        Graphics.DrawMeshInstancedProcedural(m, 0, clothMaterial, bounds, 1);

    }

    /* vec should be normalized */
    float Cot(Vector3 v1, Vector3 v2) {
        float C = Vector3.Dot(v1, v2);
        float S = Mathf.Max(Mathf.Sqrt(1 - C * C), 0.0001f);
        return C / S;
    }

    float GetS(Vector3 v1, Vector3 v2) {
        return 0.5f * Vector3.Cross(v1, v2).magnitude;
    }
}
