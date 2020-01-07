using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DrawingUtilities 
{
    public enum TrianglePoint
    {
        Top,
        Down,
        Left,
        Right
    }
    public static void DrawTriangle(Vector2 position,float size,Color col, TrianglePoint point)
    {
        Vector3[] points = new Vector3[3];
        Color oldColor = Handles.color;
        Handles.color = col;
        switch (point)
        {
            case TrianglePoint.Top:
                points[0] = position + new Vector2(-size/2,-size/2);
                points[1] = points[0] + new Vector3(size,0);
                points[2] = position + new Vector2(0, size / 2);
                break;
            case TrianglePoint.Down:
                break;
            case TrianglePoint.Left:
                break;
            case TrianglePoint.Right:
                points[0] = position + new Vector2(-size / 2, -size / 2);
                points[1] = points[0] + new Vector3(0, size);
                points[2] = position + new Vector2(size / 2, 0);
                break;
        }

        Handles.DrawAAConvexPolygon(points);
        Handles.color = oldColor;
    }

    public static void DrawLineArray(Vector3[] points,Color color)
    {
        Handles.color = color;
        for (int i = 0; i < points.Length-1; i++)
        {
            Handles.DrawLine(points[i],points[i+1]);
        }

        Handles.DrawAAPolyLine(3f,points);
    }
}
