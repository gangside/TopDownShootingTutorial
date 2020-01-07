using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Connection : Selectable
{
    public ConnectionType type;
    [NonSerialized]
    public Node fromNode = null;
    public int fromNodeID;
    private Rect lastFromNodePos = new Rect();

    [NonSerialized]
    public Node toNode = null;
    public int toNodeID;
    private Rect lastToNodePos = new Rect();

    public Vector3[] lineArray;
    public Rect controlRect;

    public void ConnectToNode(Node node)
    {
        toNode = node;
    }

    public void DisconnectFromNode(Node node)
    {
        if(toNode == node)
            toNode = null;
    }

    public Rect CalculateLineArrayBoundaries(Vector3[] array)
    {
        float minX = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        float minY = Mathf.Infinity;
        float maxY = Mathf.NegativeInfinity;
        foreach (Vector3 point in array)
        {
            if (point.x < minX)
                minX = point.x;
            if (point.x > maxX)
                maxX = point.x;
            if (point.y < minY)
                minY = point.y;
            if (point.y > maxY)
                maxY = point.y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public void Draw(SettingsData settings)
    {
        Color con_color = Color.white;

        if (hover)
            con_color = settings.connectionHoverColor;
        else
        {
            switch (type)
            {
                case ConnectionType.Association:
                    {
                        con_color = settings.associationColor;
                    }
                    break;
                case ConnectionType.Inheritance:
                    {
                        con_color = settings.inheritanceColor;
                    }
                    break;
                default:
                    con_color = settings.associationColor;
                    break;

            }
        }

        Handles.color = con_color;

        Vector2 start = fromNode.output.position.center;
        Vector2 end = toNode.input.position.center;

        #region Possible-Scenarios

        lineArray = CreateLines(settings, start, end, fromNode, toNode, 100);

        Color color = hover ? settings.connectionHoverColor : con_color;
        DrawingUtilities.DrawLineArray(lineArray, color);

        if (lastFromNodePos.x != fromNode.position.x ||
            lastFromNodePos.y != fromNode.position.y ||
            lastFromNodePos.width != fromNode.position.width ||
            lastFromNodePos.height != fromNode.position.height ||
            lastToNodePos.x != toNode.position.x ||
            lastToNodePos.y != toNode.position.y ||
            lastToNodePos.width != toNode.position.width ||
            lastToNodePos.height != toNode.position.height)
        {
            controlRect = CalculateLineArrayBoundaries(lineArray);
        }


        lastFromNodePos = fromNode.position;
        lastToNodePos = toNode.position;
    }
    #endregion

    private Vector3[] CreateLinesAlternative (SettingsData settings, Vector2 start, Vector2 end, Node from, Node to, float bezierOffset)
    {
        List<Vector3> points = new List<Vector3>();



        return points.ToArray();
    }

    private Vector3[] CreateLines(SettingsData settings,Vector2 start,Vector2 end, Node from,Node to,float bezierOffset)
    {
        Connection connection = this;
        List<Vector3> points = new List<Vector3>();
        float dist = Vector2.Distance(start, end);

        if (from.output.position.center.x > to.input.position.center.x)
        {

            if(to.position.y>from.position.y)
            {
                if (settings.useStraightConnections)
                {
                    points.Add(new Vector2(start.x, start.y));
                    points.Add(new Vector2(start.x + settings.connectionStartThreshold, start.y));
                    points.Add(new Vector2(start.x + settings.connectionStartThreshold, from.position.y + settings.connectionStartThreshold*2 + connection.fromNode.position.height));
                    points.Add(new Vector2(end.x - settings.connectionStartThreshold, from.position.y + settings.connectionStartThreshold*2 + connection.fromNode.position.height));
                    points.Add(new Vector2(end.x - settings.connectionStartThreshold, end.y));
                    points.Add(new Vector2(end.x, end.y));
                }
                else
                {
                    points.Add(new Vector2(start.x, start.y));

                    Vector3 bezierStart = new Vector2(start.x + settings.connectionStartThreshold, start.y);
                    Vector3 bezierEnd = new Vector2(start.x + settings.connectionStartThreshold, from.position.y + settings.connectionStartThreshold*2 + connection.fromNode.position.height);
                    Vector3 tangent = (bezierStart + bezierEnd) / 2 + new Vector3(settings.connectionStartThreshold, 0);
                    points = points.AddAll(Handles.MakeBezierPoints(bezierStart, bezierEnd, tangent, bezierEnd, 20));

                    bezierStart = new Vector2(end.x - settings.connectionStartThreshold, bezierEnd.y);
                    bezierEnd = new Vector2(end.x - settings.connectionStartThreshold, end.y);
                    tangent = (bezierStart + bezierEnd) / 2 - new Vector3(settings.connectionStartThreshold, 0);
                    points = points.AddAll(Handles.MakeBezierPoints(bezierStart, bezierEnd, tangent, bezierEnd, 20));

                    points.Add(new Vector2(end.x, end.y));
                }
             
            }
            else
            {
                if (settings.useStraightConnections)
                {
                    points.Add(new Vector2(start.x, start.y));
                    points.Add(new Vector2(start.x + settings.connectionStartThreshold, start.y));
                    points.Add(new Vector2(start.x + settings.connectionStartThreshold, from.position.y - settings.connectionStartThreshold));
                    points.Add(new Vector2(end.x - settings.connectionStartThreshold, from.position.y - settings.connectionStartThreshold));
                    points.Add(new Vector2(end.x - settings.connectionStartThreshold, end.y));
                    points.Add(new Vector2(end.x, end.y));
                }
                else
                {
                    points.Add(new Vector2(start.x, start.y));

                    Vector3 bezierStart = new Vector2(start.x + settings.connectionStartThreshold, start.y);
                    Vector3 bezierEnd = new Vector2(start.x + settings.connectionStartThreshold, from.position.y - settings.connectionStartThreshold);
                    Vector3 tangent = (bezierStart + bezierEnd) / 2 + new Vector3(settings.connectionStartThreshold, 0);
                    points = points.AddAll(Handles.MakeBezierPoints(bezierStart, bezierEnd, tangent, bezierEnd, 20));

                    bezierStart = new Vector2(end.x - settings.connectionStartThreshold, bezierEnd.y);
                    bezierEnd = new Vector2(end.x - settings.connectionStartThreshold, end.y);
                    tangent = (bezierStart + bezierEnd) / 2 - new Vector3(settings.connectionStartThreshold, 0);
                    points = points.AddAll(Handles.MakeBezierPoints(bezierStart, bezierEnd, tangent, bezierEnd, 20));

                    points.Add(new Vector2(end.x, end.y));
                }
            }
           

        }
        else
        {
            if (to.position.y > from.position.y)
            {
                if (settings.useStraightConnections)
                {
                    points.Add(new Vector2(start.x, start.y));
                    points.Add(new Vector2(start.x + settings.connectionStartThreshold, start.y));
                    points.Add(new Vector2(end.x - settings.connectionStartThreshold, end.y));
                    points.Add(new Vector2(end.x, end.y));
                }
                else
                {
                    if (dist < 150 || Math.Abs(start.y - end.y) < 50)
                    {
                        points.Add(new Vector2(start.x, start.y));
                        points.Add(new Vector2(start.x + settings.connectionStartThreshold, start.y));
                        points.Add(new Vector2(end.x - settings.connectionStartThreshold, end.y));
                        points.Add(new Vector2(end.x, end.y));
                    }
                    else
                    {
                        Vector2 bezierStart = new Vector2(start.x + settings.connectionStartThreshold, start.y);
                        Vector2 bezierEnd = new Vector2(end.x - settings.connectionStartThreshold, end.y);

                        Vector3 startTangent = bezierStart + Vector2.right * bezierOffset;
                        Vector3 endTangent = bezierEnd - Vector2.right * bezierOffset;

                        points.Add(new Vector2(start.x, start.y));
                        points = points.AddAll(Handles.MakeBezierPoints(bezierStart, bezierEnd, startTangent, endTangent, 60));
                        points.Add(new Vector2(end.x, end.y));
                    }
                 
                }
            }
            else
            {
                if (settings.useStraightConnections)
                {
                    points.Add(new Vector2(start.x, start.y));
                    points.Add(new Vector2(start.x + settings.connectionStartThreshold, start.y));
                    points.Add(new Vector2(end.x - settings.connectionStartThreshold, end.y));
                    points.Add(new Vector2(end.x, end.y));
                }
                else
                {


                    if (dist < 150 || Math.Abs(start.y-end.y)<50)
                    {
                        points.Add(new Vector2(start.x, start.y));
                        points.Add(new Vector2(start.x + settings.connectionStartThreshold, start.y));
                        points.Add(new Vector2(end.x - settings.connectionStartThreshold, end.y));
                        points.Add(new Vector2(end.x, end.y));
                    }
                    else
                    {
                        Vector2 bezierStart = new Vector2(start.x + settings.connectionStartThreshold, start.y);
                        Vector2 bezierEnd = new Vector2(end.x - settings.connectionStartThreshold, end.y);

                        Vector3 startTangent = bezierStart + Vector2.right * bezierOffset;
                        Vector3 endTangent = bezierEnd - Vector2.right * bezierOffset;

                        points.Add(new Vector2(start.x, start.y));
                        points = points.AddAll(Handles.MakeBezierPoints(bezierStart, bezierEnd, startTangent, endTangent, 60));
                        points.Add(new Vector2(end.x, end.y));
                    }
                  
                }
            }

           
        }

        return points.ToArray();
        //TODO: control line width with zoom value.
    }
}
[System.Serializable]
public enum ConnectionType
{
    Association,
    Inheritance
}