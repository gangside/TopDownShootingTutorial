using System.Collections.Generic;

[System.Serializable]
public class IO_Point : Selectable
{
    public string name = "IO_Point";
    [System.NonSerialized]
    public Node parentNode;
    [System.NonSerialized]
    public List<Node> connectedNodes = new List<Node>();
    [System.NonSerialized]
    public List<Connection> connections = new List<Connection>();
}