using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Edges : MonoBehaviour
{ 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DrawBounds();
    }

    void DrawBounds()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = false;
        lr.widthMultiplier = 0.02f;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.blue;


        MeshFilter mf = GetComponentInParent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("No MeshFilter found");
        }
        Bounds b = mf.mesh.bounds;

        Vector3 min = b.min;
        Vector3 max = b.max;

        Vector3[] points = new Vector3[]
        {
            // Bottom
            new Vector3(min.x,min.y,min.z),
            new Vector3(max.x,min.y,min.z),
            new Vector3(max.x,min.y,max.z),
            new Vector3(min.x,min.y,max.z),
            new Vector3(min.x,min.y,min.z),

            // Vertical
            new Vector3(min.x,max.y,min.z),
            new Vector3(max.x,max.y,min.z),
            new Vector3(max.x,min.y,min.z),

            new Vector3(max.x,max.y,max.z),
            new Vector3(max.x,min.y,max.z),

            new Vector3(min.x,max.y,max.z),
            new Vector3(min.x,min.y,max.z),
            
            // TOP
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
            new Vector3(min.x, max.y, max.z),

        };
        lr.positionCount = points.Length;
        lr.SetPositions(points);
    }

    
}
