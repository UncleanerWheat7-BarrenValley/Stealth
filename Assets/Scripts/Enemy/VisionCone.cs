using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    // Start is called before the first frame update

    public int segments = 50;
    [SerializeField]
    Enemy enemyScript;
    void Start()
    {
        Mesh visionConeMesh = GenerateVisionConeMesh();
        GetComponent<MeshFilter>().mesh = visionConeMesh;

        Color alpha = GetComponent<Renderer>().material.color;
        alpha.a = 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    Mesh GenerateVisionConeMesh()
    {
        //////////////////////////////////////////
        ///This was largly written by chatGPT
        /////////////////////////////////////////
        
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero + Vector3.up * 1.5f;
        float angleStep = enemyScript.fov.fovValue / segments;
        float angle = transform.eulerAngles.y - enemyScript.fov.fovValue / 2f;

        for (int i = 1; i <= segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * angle; // Use the angle directly without adding transform.eulerAngles.y

            Vector3 vertex = (transform.position + Vector3.up * 1.5f) + new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad)) * enemyScript.fov.depthOfViewValue;

            vertices[i] = transform.InverseTransformPoint(vertex);

            if (i < segments)
            {
                triangles[(i - 1) * 3] = 0;
                triangles[(i - 1) * 3 + 1] = i;
                triangles[(i - 1) * 3 + 2] = i + 1;
            }
            else // For the last segment, connect it with the first vertex to close the loop
            {
                triangles[(i - 1) * 3] = 0;
                triangles[(i - 1) * 3 + 1] = i;
                triangles[(i - 1) * 3 + 2] = 1; // Connect with the first vertex
            }

            angle += angleStep;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
