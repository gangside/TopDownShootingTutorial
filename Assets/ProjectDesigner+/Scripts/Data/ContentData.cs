using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentData : ScriptableObject {
    public List<Node> activeNodes = new List<Node>();
    public List<Group> activeGroups = new List<Group>();
    public List<Connection> activeConnections = new List<Connection>();

    public int targetConnectionID = -1;
    public int targetNodeID = -1;
    public int selectedPointID = -1;
}
