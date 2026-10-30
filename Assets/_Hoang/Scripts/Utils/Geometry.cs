using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Geometry
{
    public static bool LinesIntersection2D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, bool withinLine1, bool withinLine2, out Vector3 intersection)
    {
        intersection = Vector3.zero;

        Vector3 d1 = p2 - p1;
        Vector3 d2 = p4 - p3;

        float det = (-d1.x * d2.y + d1.y * d2.x);
        if (Mathf.Abs(det) < Mathf.Epsilon) return false;

        Vector3 diff = p3 - p1;
        float t = (-d2.y * diff.x + d2.x * diff.y) / det;
        float u = (-d1.y * diff.x + d1.x * diff.y) / det;

        if (withinLine1 && (t < 0 || t > 1)) return false;
        if (withinLine2 && (u < 0 || u > 1)) return false;

        intersection = p1 + t * d1;
        return true;
    }

    public static bool LineRectIntersection2D(Vector3 p1, Vector3 p2, Vector3 center, Vector3 size, float rotation, out Vector3 intersection)
    {
        Vector3[] corners = GetRectCorners(center, size, rotation);
        return LineRectIntersection2D(p1, p2, corners, out intersection);
    }

    public static bool LineRectIntersection2D(Vector3 p1, Vector3 p2, Vector3[] corners, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        float minDistance = float.MaxValue;
        int count = 0;

        for (int i = 0; i < 4; i++)
        {
            Vector3 p3 = corners[i];
            Vector3 p4 = corners[(i + 1) % 4];
            if (LinesIntersection2D(p1, p2, p3, p4, false, true, out Vector3 temp))
            {
                count++;
                float distance = Vector3.Distance(p1, temp);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    intersection = temp;
                }
                if (count == 2) break;
            }
        }

        return count > 0;
    }

    public static Vector3[] GetRectCorners(Vector3 center, Vector3 size, float rotation)
    {
        Vector3 halfSize = 0.5f * size;
        Quaternion rot = Quaternion.Euler(0, 0, rotation);
        return new Vector3[]
        {
            center + rot * new Vector3(-halfSize.x, halfSize.y),
            center + rot * new Vector3(-halfSize.x, -halfSize.y),
            center + rot * new Vector3(halfSize.x, -halfSize.y),
            center + rot * new Vector3(halfSize.x, halfSize.y)
        };
    }

    public static Vector3[] GetBoxColliderCorners(BoxCollider2D boxCollider, Transform objTransform, float scale)
    {
        // Get the size and offset of the BoxCollider2D
        Vector2 size = scale * boxCollider.size;
        Vector2 offset = scale * boxCollider.offset;

        // Get the transform's position and rotation
        Vector2 position = objTransform.TransformPoint(offset); // Account for the collider's offset
        Quaternion rotation = objTransform.rotation;

        // Calculate local corners
        Vector2 halfSize = size * 0.5f;
        Vector2[] localCorners = new Vector2[4]
        {
            new Vector2(-halfSize.x, halfSize.y),  // Top-left
            new Vector2(halfSize.x, halfSize.y),    // Top-right
            new Vector2(halfSize.x, -halfSize.y),  // Bottom-right
            new Vector2(-halfSize.x, -halfSize.y), // Bottom-left
        };

        // Transform local corners to world space
        Vector3[] worldCorners = new Vector3[4];
        for (int i = 0; i < localCorners.Length; i++)
        {
            worldCorners[i] = position + (Vector2)(rotation * localCorners[i]);
        }

        return worldCorners;
    }

    public static bool PointLineProjection2D(Vector3 p, Vector3 a, Vector3 b, bool withinLine, out Vector3 projected)
    {
        projected = Vector3.zero;
        Vector3 direction = b - a;
        float length = direction.sqrMagnitude;

        if (Mathf.Approximately(length, 0f))
        {
            return false;
        }

        Vector3 ap = p - a;
        float t = Vector3.Dot(ap, direction) / length;

        if (withinLine && (t < 0 || t > 1)) return false;

        projected = a + t * direction;
        return true;
    }

    public static Vector2 Rotate(Vector2 original, float rotation)
    {
        float angleInRadians = rotation * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(angleInRadians);
        float sinTheta = Mathf.Sin(angleInRadians);

        float x = original.x * cosTheta - original.y * sinTheta;
        float y = original.x * sinTheta + original.y * cosTheta;

        return new Vector2(x, y);
    }

    public static bool ArePerpendicular2D(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(Vector2.Dot(a, b), 0f);
    }

    public static bool AreParallel2D(Vector3 a, Vector3 b)
    {
        return Mathf.Approximately(a.x * b.y - a.y * b.x, 0f);
    }

    public int CheckVectorRelation2D(Vector3 a, Vector3 b)
    {
        if (ArePerpendicular2D(a, b)) return 1;
        else if (AreParallel2D(a, b)) return 0;
        else return -1;
    }

    public static Vector3 GetLocalPos(Vector3 pos, float rotation)
    {
        return Quaternion.Euler(0, 0, rotation) * pos;
    }

    public static float CalculatePolygonArea(PolygonCollider2D polygon)
    {
        float fullArea = -1;
        List<float> holeAreaList = new();

        for (int i = 0; i < polygon.pathCount; i++)
        {
            Vector2[] points = polygon.GetPath(i);
            float area = CalculateArea(points);
            if (fullArea < area)
            {
                if (fullArea > 0)
                {
                    holeAreaList.Add(area);
                }
                fullArea = area;
            }
            else
            {
                holeAreaList.Add(area);
            }
        }

        for (int i = 0; i < holeAreaList.Count; i++)
        {
            fullArea -= holeAreaList[i];
        }

        return fullArea;
    }

    static float CalculateArea(Vector2[] points)
    {
        float area = 0;
        int pointCount = points.Length;

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 current = points[i];
            Vector2 next = points[(i + 1) % pointCount];
            area += (current.x * next.y) - (current.y * next.x);
        }

        area *= 0.5f;
        return Mathf.Abs(area);
    }
}
