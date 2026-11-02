using UnityEngine;

public class GizmosUtils
{
    public static void DrawTable(int width, int height, Vector3 center, Vector3 cellSize)
    {
        Vector3 root = new Vector3(-(float)width / 2, -(float)height / 2);

        for (int x = 0; x <= width; x++)
        {
            Vector3 start = root + new Vector3(x, 0f, 0f);
            start = Vector3.Scale(start, cellSize) + center;

            Vector3 end = root + new Vector3(x, height, 0f);
            end = Vector3.Scale(end, cellSize) + center;

            Gizmos.DrawLine(start, end);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 start = root + new Vector3(0f, y, 0f);
            start = Vector3.Scale(start, cellSize) + center;

            Vector3 end = root + new Vector3(width, y, 0f);
            end = Vector3.Scale(end, cellSize) + center;

            Gizmos.DrawLine(start, end);
        }
    }

    public static void DrawPath(params Vector3[] path)
    {
        for (int i = 0; i < path.Length - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }
}
