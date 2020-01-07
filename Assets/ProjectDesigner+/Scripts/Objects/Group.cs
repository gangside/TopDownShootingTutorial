using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Group : Selectable
{
    public string header;

    public bool editHeader;
    public string groupComment = "Group Comment section. Bla bla bla bla bal etc";
    public string groupDescription = "Group Description. Bla bla bla bla bal etc";

    public bool showComment = true;
    [System.NonSerialized]
    public List<Node> childNodes = new List<Node>();

    public List<int> childIDs = new List<int>();


    public Group(Rect _position, string _header)
    {
        position = _position;
        header = _header;
        
    }
    public Group(Group origin)
    {
        position = origin.position;
        header = origin.header;
        baseColor = origin.baseColor;
        outlineColor = origin.outlineColor;
    }

    public void AddNode(Node nodeToAdd)
    {
        if (!childNodes.Contains(nodeToAdd))
        {
            childNodes.Add(nodeToAdd);
        }
        else
        {
            Debug.Log("Node already present in this card!");
        }
    }

    public void RemoveNode(Node nodeToRemove)
    {
        if (childNodes.Contains(nodeToRemove))
        {
            childNodes.Remove(nodeToRemove);
        }
        else
        {
            Debug.Log("Node does not present in this card!");
        }
    }
}
