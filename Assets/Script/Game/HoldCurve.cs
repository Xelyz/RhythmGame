using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HoldCurve : MonoBehaviour
{
    int segmentCount = 50; // 曲线分段数
    internal float width = 2f; // 曲线的宽度
    int arcSegmentCount = 32; // 圆弧分段数

    private Mesh mesh;

    public void RenderPath(List<SlideSegment> segments)
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        foreach (SlideSegment segment in segments)
        {
            AddCurveMesh(segment);
            AddEndCap(segment, false); // 添加终点圆弧
        }
        AddEndCap(segments[0], true); // 添加起点圆弧
        mesh.RecalculateNormals();
    }

    void AddCurveMesh(SlideSegment segment)
    {
        // Retrieve existing vertices and triangles from the current mesh
        Vector3[] oldVertices = mesh.vertices;
        int[] oldTriangles = mesh.triangles;
        Vector2[] oldUVs = mesh.uv;

        int oldVertexCount = oldVertices.Length;
        int oldTriangleCount = oldTriangles.Length;
        int oldUVCount = oldUVs.Length;

        // Create new vertices, triangles, and UVs for the new curve segment
        Vector3[] newVertices = new Vector3[(segmentCount + 1) * 2];
        int[] newTriangles = new int[segmentCount * 6];
        Vector2[] newUVs = new Vector2[(segmentCount + 1) * 2];

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount * segment.maxT;
            Vector3 curvePoint = segment.PointAt(t);

            // Calculate direction normal to the curve to generate width
            Vector3 direction;
            Vector3 prevPoint = segment.PointAt(Mathf.Max(0, t - 0.001f));
            Vector3 nextPoint = segment.PointAt(Mathf.Min(segment.maxT, t + 0.001f));
            direction = (prevPoint - nextPoint).normalized;

            Vector3 normal = Vector3.Cross(direction, Vector3.forward).normalized;

            newVertices[i * 2] = curvePoint + width * 0.5f * normal;  // Top vertex
            newVertices[i * 2 + 1] = curvePoint - width * 0.5f * normal;  // Bottom vertex

            newUVs[i * 2] = new Vector2(t, 1);  // Top UV
            newUVs[i * 2 + 1] = new Vector2(t, 0);  // Bottom UV

            if (i < segmentCount)
            {
                int startIndex = i * 6;
                int vertexOffset = oldVertexCount + i * 2;

                newTriangles[startIndex] = vertexOffset;
                newTriangles[startIndex + 1] = vertexOffset + 1;
                newTriangles[startIndex + 2] = vertexOffset + 2;
                newTriangles[startIndex + 3] = vertexOffset + 1;
                newTriangles[startIndex + 4] = vertexOffset + 3;
                newTriangles[startIndex + 5] = vertexOffset + 2;
            }
        }

        // Combine old and new vertices, UVs
        Vector3[] combinedVertices = new Vector3[oldVertexCount + newVertices.Length];
        oldVertices.CopyTo(combinedVertices, 0);
        newVertices.CopyTo(combinedVertices, oldVertexCount);

        Vector2[] combinedUVs = new Vector2[oldUVCount + newUVs.Length];
        oldUVs.CopyTo(combinedUVs, 0);
        newUVs.CopyTo(combinedUVs, oldUVCount);

        // Combine old and new triangles
        int[] combinedTriangles = new int[oldTriangleCount + newTriangles.Length];
        oldTriangles.CopyTo(combinedTriangles, 0);
        for (int i = 0; i < newTriangles.Length; i++)
        {
            combinedTriangles[oldTriangleCount + i] = newTriangles[i];
        }

        // Update the mesh with the combined data
        mesh.vertices = combinedVertices;
        mesh.triangles = combinedTriangles;
        mesh.uv = combinedUVs;
    }

    void AddEndCap(SlideSegment segment, bool isStart)
    {
        Vector3[] oldVertices = mesh.vertices;
        int[] oldTriangles = mesh.triangles;
        Vector2[] oldUVs = mesh.uv;

        int oldVertexCount = oldVertices.Length;
        int oldTriangleCount = oldTriangles.Length;
        int oldUVCount = oldUVs.Length;

        float t = isStart ? 0 : segment.maxT;
        Vector3 curvePoint = segment.PointAt(t);
        Vector3 direction = isStart ? (segment.PointAt(t + 0.001f) - curvePoint).normalized : (curvePoint - segment.PointAt(t - 0.001f)).normalized;
        Vector3 normal = Vector3.Cross(direction, Vector3.forward).normalized;

        float start_angle = Mathf.Atan2(normal.y, normal.x);

        Vector3[] newVertices = new Vector3[arcSegmentCount + 1];
        int[] newTriangles = new int[arcSegmentCount * 3];
        Vector2[] newUVs = new Vector2[arcSegmentCount + 1];

        newVertices[0] = curvePoint; // Center point of the arc
        newUVs[0] = new Vector2(0.5f, 0.5f); // Center UV

        for (int i = 0; i <= arcSegmentCount; i++)
        {
            float angle = start_angle - (isStart ? 1 : -1) * Mathf.PI * i / arcSegmentCount; // 半圆弧角度
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (width * 0.5f);
            newVertices[i] = curvePoint + offset;
            newUVs[i] = new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f); // UV coordinates for the arc

            if (i < arcSegmentCount)
            {
                int startIndex = i * 3;
                newTriangles[startIndex] = oldVertexCount;
                newTriangles[startIndex + 1] = oldVertexCount + i;
                newTriangles[startIndex + 2] = oldVertexCount + i + 1;
            }
        }

        // Combine old and new vertices, UVs
        Vector3[] combinedVertices = new Vector3[oldVertexCount + newVertices.Length];
        oldVertices.CopyTo(combinedVertices, 0);
        newVertices.CopyTo(combinedVertices, oldVertexCount);

        Vector2[] combinedUVs = new Vector2[oldUVCount + newUVs.Length];
        oldUVs.CopyTo(combinedUVs, 0);
        newUVs.CopyTo(combinedUVs, oldUVCount);

        // Combine old and new triangles
        int[] combinedTriangles = new int[oldTriangleCount + newTriangles.Length];
        oldTriangles.CopyTo(combinedTriangles, 0);
        for (int i = 0; i < newTriangles.Length; i++)
        {
            combinedTriangles[oldTriangleCount + i] = newTriangles[i];
        }

        // Update the mesh with the combined data
        mesh.vertices = combinedVertices;
        mesh.triangles = combinedTriangles;
        mesh.uv = combinedUVs;
    }
}
