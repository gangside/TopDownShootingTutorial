using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

/// <summary>
/// ProjectDesigner+
/// A Birch Games product.
/// Brought to you by Civelek Babacik.
/// Instructions:
/// --Any setting that can be manipulated by user will be put in SettingsData.cs, don't forget to save and load if necessary.
///     --We read settings file to "SettingsData settings" field. And use it for variables in realtime.
/// --Any content to be saved, in order to continue the graph later, will be stored by ContentData scriptable object.
/// --To generate scripts based on relations, use GenerateScript.cs
/// --To edit zoom behaviour of the Graph Panel, use RectExtensions.cs
/// --To change data types of fields-methods Look at DataTypes.cs
/// </summary>
public class NodeEditor : EditorWindow
{

    private bool TERMINATE = false;
    //Fields
    #region Fields

    #region Settings
    public static SettingsData settings;
    #endregion

    #region Containers
    Node[] nodeBuffer;
    Node targetNode;
    Node markedForDeselect;

    Group targetGroup;//the group that has been clicked on
    Group parentTargetGroup;//the group that has been targeted by dragging nodes

    List<Node> activeNodes = new List<Node>();
    List<Node> selectedNodes = new List<Node>();
    List<Node> visibleNodes = new List<Node>();

    List<Connection> activeConnections = new List<Connection>();

    List<Group> activeGroups = new List<Group>();

    #endregion

    #region Logic
    bool drawSelectionRect;

    bool drawLines;

    bool dragging;

    bool clickedOnSomething;

    bool clickedOnNode;

    bool resizeMode;

    IO_Point selectedPoint;
    IO_Point tempSelectionPoint;//For the auto-connect context menu

    Vector2 mousePos;

    Vector2 mouseDragStartPosition;

    Vector2 closestGridPoint;
    Node closestNode;

    Connection targetConnection = null;

    GUISkin globalSkinScene,globalSkinInspector;
    #endregion

    #region Options Panel
    Vector2 undoScrollPos, optionsScrollPosition, debug_groupPanelScrollPos;

    bool showSaveLoad;

    bool showGridOptions;

    bool showNodeOptions;

    bool showGroupOptions;

    bool showHints;

    bool showScreenshotOptions;

    bool debugWindow;

    bool debugShowUndoRedo;

    bool showSelectedNodes;

    bool showActiveGroups;

    //bool showSelectedGroups;
    #endregion

    #region Panel
    float minZoomValue = 1f;
    float maxZoomValue = 2f;

    Rect optionsPanel;

    Rect graphPanel;
    Vector2 graphCenter;

    Rect selectionRect;

    List<Vector2> gridPoints = new List<Vector2>();

    float last_XPanelSize;
    float last_YPanelSize;
    float lastGridSpacing;
    float lastZoomvalue;
    float lastXoffset;
    float lastYoffset;
    #endregion

    #region Screenshot
    Rect screenshotBorder;

    Vector2 lastCenter = new Vector2(0, 0);

    float xOffset;
    float yOffset;

    bool screenshotMode;
    int horizontalTakes;
    int verticalTakes;

    Texture2D screenshot;

    int curTake;
    float oldZoomValue;
    Vector2 oldOffset;

    float screenshotTimer;

    bool releasingConnection;
    #endregion

    #region Save & Load
    bool saveOnlyNodes;
    bool loadButton;
    int curLoadIndex;

    public static string settingsPath = "Assets/ProjectDesigner+/Data/Settings/EditorSettings.asset";
    #endregion

    #region Undo & Redo
    private List<ContentData> history = new List<ContentData>();
    private int historyIndex = 0;
    #endregion


    public static Font globalFont;
    private static string fontPath = "Assets/ProjectDesigner+/Data/Fonts/DefaultFont.ttf";
    private static TexturePack texturePack;

    private static float activeOptionsPanelOffset;

    private long gridDrawTime, nodesDrawTime, optionsPanelDrawTime, connectionsDrawTime;

    private static bool recordCurrentState = false;

    private static GUIStyle[] customStyles;

    private static GUIStyle buttonStyle;

    private static GUIStyle hintsStyle;

    private static GUIStyle fontStyle;

    private static GUIStyle groupBorder;
    private static GUIStyle groupBorderSelected;

    private static GUIStyle toggle;

    #endregion
    //Fields-End

    #region Undo - Redo
    public void recordUndo()
    {
        ContentData data = CreateInstance<ContentData>();

        for (int i = 0; i < activeNodes.Count; i++)
        {
            activeNodes[i].ID = i;
            if (activeNodes[i].parentGroup != null)
            {
                if (activeNodes[i].parentGroup.childIDs == null)
                {
                    activeNodes[i].parentGroup = null;
                    continue;
                }
                activeNodes[i].parentGroup.childIDs.Add(i);
            }
            activeNodes[i].input.ID = i;
            activeNodes[i].output.ID = i + activeNodes.Count;
        }

        for (int i = 0; i < activeConnections.Count; i++)
        {
            activeConnections[i].ID = i;
            activeConnections[i].fromNodeID = activeConnections[i].fromNode.ID;
            activeConnections[i].toNodeID = activeConnections[i].toNode.ID;
        }

        foreach (Node node in activeNodes)
        {
            if (node.parentNode != null)
            {
                node.parentNodeID = node.parentNode.ID;
            }
            data.activeNodes.Add(new Node(node,false));
        }

        foreach (Group group in activeGroups)
        {
            data.activeGroups.Add(group);
        }

        foreach (Connection con in activeConnections)
        {
            data.activeConnections.Add(con);
        }

        if (targetConnection != null)
        {
            data.targetConnectionID = targetConnection.ID;
        }
        if (targetNode != null)
        {
            data.targetNodeID = targetNode.ID;
        }
        if (selectedPoint != null)
        {
            data.selectedPointID = selectedPoint.ID;
        }

        if (historyIndex < history.Count - 1)
        {
            history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
        }

        history.Add(data);
        historyIndex = history.IndexOf(data);
    }

    public void loadRecord()
    {
        ContentData data = history[historyIndex];

        targetConnection = null;
        targetNode = null;
        selectedPoint = null;

        selectedNodes.Clear();

        activeNodes.Clear();
        activeGroups.Clear();
        activeConnections.Clear();

        for (int i = 0; i < data.activeNodes.Count; i++)
        {
            Node newNode = new Node(data.activeNodes[i], false);
            activeNodes.Add(newNode);
            if (data.activeNodes[i].selected)
            {
                SelectNode(newNode);
            }
            if (data.selectedPointID == data.activeNodes[i].input.ID)
            {
                selectedPoint = data.activeNodes[i].input;
            }
            if (data.selectedPointID == data.activeNodes[i].output.ID)
            {
                selectedPoint = data.activeNodes[i].output;
            }
            if (data.targetNodeID == data.activeNodes[i].ID)
            {
                targetNode = newNode;
            }
            if (newNode.parentNodeID != -1)
            {
                newNode.parentNodeID = data.activeNodes[i].parentNodeID;
            }
        }

        foreach (Node node in activeNodes)
        {
            if (node.parentNodeID != -1)
            {
                node.parentNode = activeNodes[node.parentNodeID];
            }
        }

        for (int i = 0; i < data.activeConnections.Count; i++)
        {
            Connection con = connectNodes(activeNodes[data.activeConnections[i].fromNodeID], activeNodes[data.activeConnections[i].toNodeID], data.activeConnections[i].type, false);
            if (data.targetConnectionID == data.activeConnections[i].ID)
            {
                targetConnection = con;
            }
        }

        for (int i = 0; i < data.activeGroups.Count; i++)
        {
            Group newCard = new Group(data.activeGroups[i]);
            for (int j = 0; j < data.activeGroups[i].childIDs.Count; j++)
            {
                newCard.AddNode(activeNodes[data.activeGroups[i].childIDs[j]]);
                activeNodes[data.activeGroups[i].childIDs[j]].parentGroup = newCard;
            }
            activeGroups.Add(newCard);
        }
    }


    public void Undo()
    {
        if (historyIndex > 0)
            historyIndex--;
        loadRecord();
    }

    public void Redo()
    {
        if (historyIndex < history.Count - 1)
        {
            historyIndex++;
        }
        loadRecord();
    }
    #endregion

    #region Save & Load
    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    public void saveContents(string path)
    {
        ContentData data = CreateInstance<ContentData>();

        for (int i = 0; i < activeNodes.Count; i++)
        {
            activeNodes[i].ID = i;
            if (activeNodes[i].parentGroup != null)
            {
                if (activeNodes[i].parentGroup.childIDs == null)
                {
                    activeNodes[i].parentGroup = null;
                    continue;
                }
                activeNodes[i].parentGroup.childIDs.Add(i);
            }
            activeNodes[i].input.ID = i;
            activeNodes[i].output.ID = i + activeNodes.Count;
        }

        for (int i = 0; i < activeConnections.Count; i++)
        {
            activeConnections[i].ID = i;
            activeConnections[i].fromNodeID = activeConnections[i].fromNode.ID;
            activeConnections[i].toNodeID = activeConnections[i].toNode.ID;
        }

        foreach (Node node in activeNodes)
        {
            if (node.parentNode != null)
            {
                node.parentNodeID = node.parentNode.ID;
            }
            data.activeNodes.Add(new Node(node, false));
        }

        foreach (Group group in activeGroups)
        {
            data.activeGroups.Add(group);
        }

        foreach (Connection con in activeConnections)
        {
            data.activeConnections.Add(con);
        }

        if (targetConnection != null)
        {
            data.targetConnectionID = targetConnection.ID;
        }
        if (targetNode != null)
        {
            data.targetNodeID = targetNode.ID;
        }
        if (selectedPoint != null)
        {
            data.selectedPointID = selectedPoint.ID;
        }

        int curIndex = 1;
        string originalPath = path;
        while (File.Exists(path + ".asset"))
        {
            path = originalPath + "(" + curIndex + ")";
        }

        AssetDatabase.CreateAsset(data, path + ".asset");
    }

    public void loadContents(string path)
    {
        if (path == "")
        {
            UnityEngine.Debug.Log("Could not load content: Path is empty!");
            return;
        }
        if (!File.Exists(path))
        {
            UnityEngine.Debug.Log("Could not load content: Invalid Path!");
            return;
        }

        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        if (AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(ContentData))
        {
            UnityEngine.Debug.Log("Could not load content: Invalid File Type!");
            return;
        }
        ContentData data = (ContentData)AssetDatabase.LoadAssetAtPath(path, typeof(ContentData));

        targetConnection = null;
        targetNode = null;
        selectedPoint = null;

        selectedNodes.Clear();

        activeNodes.Clear();
        activeGroups.Clear();
        activeConnections.Clear();

        for (int i = 0; i < data.activeNodes.Count; i++)
        {
            Node newNode = new Node(data.activeNodes[i], false);
            activeNodes.Add(newNode);
            if (data.activeNodes[i].selected)
            {
                SelectNode(newNode);
            }
            if (data.selectedPointID == data.activeNodes[i].input.ID)
            {
                selectedPoint = data.activeNodes[i].input;
            }
            if (data.selectedPointID == data.activeNodes[i].output.ID)
            {
                selectedPoint = data.activeNodes[i].output;
            }
            if (data.targetNodeID == data.activeNodes[i].ID)
            {
                targetNode = newNode;
            }
            if (newNode.parentNodeID != -1)
            {
                newNode.parentNodeID = data.activeNodes[i].parentNodeID;
            }
        }

        foreach (Node node in activeNodes)
        {
            if (node.parentNodeID != -1)
            {
                node.parentNode = activeNodes[node.parentNodeID];
            }
        }

        for (int i = 0; i < data.activeConnections.Count; i++)
        {
            Connection con = connectNodes(activeNodes[data.activeConnections[i].fromNodeID], activeNodes[data.activeConnections[i].toNodeID], data.activeConnections[i].type, false);
            if (data.targetConnectionID == data.activeConnections[i].ID)
            {
                targetConnection = con;
            }
        }

        for (int i = 0; i < data.activeGroups.Count; i++)
        {
            Group newCard = new Group(data.activeGroups[i]);
            for (int j = 0; j < data.activeGroups[i].childIDs.Count; j++)
            {
                newCard.AddNode(activeNodes[data.activeGroups[i].childIDs[j]]);
                activeNodes[data.activeGroups[i].childIDs[j]].parentGroup = newCard;
            }
            activeGroups.Add(newCard);
        }
    }

    public static void resetSettings()
    {
        settings = CreateInstance<SettingsData>();
    }

    public static void loadSettings()
    {
        settings = Instantiate(getCurrentSettings());
    }

    public static SettingsData getCurrentSettings()
    {
        if (!File.Exists(settingsPath))
        {
            SettingsData curData = CreateInstance<SettingsData>();
            AssetDatabase.CreateAsset(curData, settingsPath);
        }
        SettingsData data = (SettingsData)AssetDatabase.LoadAssetAtPath(settingsPath, typeof(SettingsData));

        return data;
    }

    public static void saveSettings()
    {
        AssetDatabase.CreateAsset(settings, settingsPath);
    }

    private string checkFileName(string path)
    {
        int id = 1;
        string originalPath = path;
        while (File.Exists(path + ".asset"))
        {
            string extension = "(" + id + ")";
            path = originalPath + extension;
            id++;
        }
        return path;
    }

    #endregion

    #region Screenshot
    private void TakeScreenshot()
    {
        screenshotMode = true;
        oldZoomValue = settings.zoomValue;
        oldOffset = settings.offset;

        settings.zoomValue = 1f;
        curTake = 0;
        screenshotTimer = 0f;

        horizontalTakes = Mathf.CeilToInt(settings.xGraphPanelSize / position.width);
        verticalTakes = Mathf.CeilToInt(settings.yGraphPanelSize / position.height);
        settings.offset.Set(0f, 0f);
        screenshot = new Texture2D((int)settings.xGraphPanelSize, (int)settings.yGraphPanelSize, TextureFormat.RGB24, false);

        screenshotBorder = new Rect(0, 0, settings.xGraphPanelSize, settings.yGraphPanelSize);
    }

    private void TakeGroupScreenshot(object data)
    {
        Group group = (Group)data;
        screenshotMode = true;
        oldZoomValue = settings.zoomValue;
        oldOffset = settings.offset;

        settings.zoomValue = 1f;
        curTake = 0;
        screenshotTimer = 0f;

        horizontalTakes = Mathf.CeilToInt(group.position.size.x / position.width);
        verticalTakes = Mathf.CeilToInt(group.position.size.y / position.height);
        settings.offset.Set(-group.position.position.x, -group.position.position.y);
        screenshot = new Texture2D((int)group.position.size.x, (int)group.position.size.y, TextureFormat.RGB24, false);
        UnityEngine.Debug.Log("Vertical Takes : " + verticalTakes + ", Horizontal Takes :" + horizontalTakes + " , Total Takes : " + verticalTakes * horizontalTakes);

        screenshotBorder = group.position;
        UnityEngine.Debug.Log("Group Rect: " + group.position);
    }
    #endregion

    #region Zoom
    private void setZoom(float newZoom)
    {
        settings.zoomValue = newZoom;

        settings.zoomValue = Mathf.Clamp(settings.zoomValue, minZoomValue, maxZoomValue);

        zoomCompensate();
    }

    private void increaseZoom(float amount)
    {
        settings.zoomValue += amount;
        settings.zoomValue = Mathf.Clamp(settings.zoomValue, minZoomValue, maxZoomValue);
        zoomCompensate();
    }

    private void zoomCompensate()
    {
        Vector2 newCenter = new Vector2((position.width - activeOptionsPanelOffset) / 2, position.height / 2);

        newCenter = (newCenter - settings.offset) / settings.zoomValue;

        settings.offset += (newCenter - lastCenter) * settings.zoomValue;
    }
    #endregion

    #region Node/Connection Operations

    private void moveNodesToGroup(Node[] nodes, Group group)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].parentGroup != null)
            {
                nodes[i].parentGroup.childNodes.Remove(nodes[i]);
                if (nodes[i].parentGroup.childNodes.Count == 0)
                {
                    activeGroups.Remove(nodes[i].parentGroup);
                }
                nodes[i].parentGroup = null;
            }
            group.childNodes.Add(nodes[i]);
            nodes[i].parentGroup = group;
        }
        recordUndo();
    }

    private void ContextDeleteGroup()
    {
        foreach (Node curNode in targetGroup.childNodes)
        {
            curNode.parentGroup = null;
        }
        targetGroup.childNodes.Clear();
        activeGroups.Remove(targetGroup);
        recordUndo();
    }

    private void ContextDuplicateGroup()
    {
        Rect newPos = new Rect(0f, 0f, 0f, 0f);
        Group newGroup = new Group(newPos, targetGroup.header + "(Copy)");
        activeGroups.Add(newGroup);

        Node[] duplicates = duplicateNodes(targetGroup.childNodes.ToArray(), targetGroup.position.size.x, 0f);
        moveNodesToGroup(duplicates, newGroup);//records undo
    }

    private void ContextRemoveNodesFromGroup()
    {
        foreach (Node curNode in selectedNodes)
        {
            if (curNode.parentGroup != null)
            {
                curNode.parentGroup.childNodes.Remove(curNode);
                curNode.parentGroup = null;
            }
        }
        recordUndo();
    }

    private void AddNodeToGroup(Node node, Group group)
    {
        node.parentGroup = group;
        group.childNodes.Add(node);
        recordUndo();
    }

    /// <summary>
    /// Use moveNodesToGroup instead!!
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="group"></param>
    private void AddNodesToGroup(Node[] nodes, Group group)
    {
        foreach (Node node in nodes)
        {
            if (!group.childNodes.Contains(node))
            {
                group.childNodes.Add(node);
            }
            node.parentGroup = group;
        }
        recordUndo();
    }

    private void RemoveNodeFromGroup(Node node)
    {
        if (node.parentGroup != null)
        {
            node.parentGroup.childNodes.Remove(node);
            node.parentGroup = null;
        }
        recordUndo();
    }

    private void RemoveNodesFromGroup(Node[] nodes)
    {
        foreach (Node curNode in nodes)
        {
            if (curNode.parentGroup != null)
            {
                curNode.parentGroup.childNodes.Remove(curNode);
                curNode.parentGroup = null;
            }
        }
        recordUndo();
    }

    private void ContextCreateGroup()
    {
        //TODO:Check if there is a card
        int cardCount = CardCountInNodes(selectedNodes);

        if (cardCount == 0)
            CreateSimpleCard(selectedNodes,"New Group");
        else if (cardCount == 1)
        {
            //if each node has the same group and node count is equals to group node count> create a new card
            List<Group> cards = GetCardsFromNodes(selectedNodes);
            activeGroups.Remove(cards[0]);
            CreateSimpleCard(selectedNodes, "New Group");

        }
        else
        {
            List<Group> cards = GetCardsFromNodes(selectedNodes);
            foreach (Group card in cards)
                activeGroups.Remove(card);
            CreateSimpleCard(selectedNodes, "New Group");

        }
        recordUndo();
    }

    private void CreateSimpleCard(List<Node> lst,string header)
    {
        Group newCard = new Group(new Rect(), header);
        activeGroups.Add(newCard);
        if(lst==null)
            newCard.childNodes = new List<Node>();
        else
        {
            newCard.childNodes = new List<Node>(lst);
            foreach (var t in lst)
                t.parentGroup = newCard;
        }
            
    }

    private void DrawGroup(Group group)
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        float maxY = Mathf.NegativeInfinity;
        for (int i = 0; i < group.childNodes.Count; i++)
        {
            if (group.childNodes[i].position.x < minX)
            {
                minX = group.childNodes[i].position.x;
            }
            if (group.childNodes[i].position.y < minY)
            {
                minY = group.childNodes[i].position.y;
            }
            if (group.childNodes[i].position.x + group.childNodes[i].position.width > maxX)
            {
                maxX = group.childNodes[i].position.x + group.childNodes[i].position.width;
            }
            if (group.childNodes[i].position.y + group.childNodes[i].position.height > maxY)
            {
                maxY = group.childNodes[i].position.y + group.childNodes[i].position.height;
            }
        }
        group.position.Set(minX - settings.connectionStartThreshold - 60, minY - 95f, maxX - minX + settings.connectionStartThreshold * 2 + 90, maxY - minY + 175f);
       
        GUIStyle border = parentTargetGroup == group
            ? groupBorder//Control highlight
            : groupBorderSelected;//LightmapEditorSelectedHighlight
        border.font = EditorStyles.centeredGreyMiniLabel.font;

        GUI.BeginGroup(group.position, new GUIContent(""), border);
        GUI.EndGroup();
        GUI.color = settings.cardBgColor;
        GUI.BeginGroup(group.position, new GUIContent(""), fontStyle);//flow node 0
        GUI.color = new Color(1, 1, 1);
        Rect labelArea = new Rect(group.position.size.x / 4, 0f, group.position.size.x / 2, 50f);
        if (!group.editHeader)
            GUI.Label(labelArea, group.header, fontStyle);
        else
            group.header = GUI.TextField(labelArea, group.header, fontStyle);
        GUI.EndGroup();

        GUI.color = Color.white;
        GUIStyle titleStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            wordWrap = true,
            font = globalFont,
            fontSize = 11
        };
       
        Rect descriptionArea = new Rect(group.position.TopRight().x + 15,
            group.position.TopRight().y,
            250, (group.position.height/2)-5);
        Rect commentsArea = new Rect(group.position.TopRight().x + 15,
            10+group.position.TopRight().y+ group.position.height / 2,
            250, (group.position.height / 2)-10);
        Rect commentToggleArea = new Rect(new Vector2(group.position.TopRight().x - 120,
                group.position.TopRight().y + 50),
            new Vector2(100, 18));

        if (group.showComment)
        {
        
           GUI.color = settings.cardBgColor;

           GUI.BeginGroup(descriptionArea, new GUIContent(""), fontStyle);
            GUI.EndGroup();
           GUI.BeginGroup(commentsArea, new GUIContent(""), fontStyle);
           GUI.EndGroup();
           GUI.color = Color.white;
           GUI.BeginGroup(descriptionArea, new GUIContent(""), border);
           Rect descriptionTitle = new Rect(10, 0, descriptionArea.width - 20, 30);
           GUI.Label(descriptionTitle, "Group Description", fontStyle);

           Rect descriptionBody = new Rect(6, 36, descriptionArea.width - 12, -40 + group.position.height / 2);
           if (!group.editHeader)
               GUI.Label(descriptionBody, group.groupDescription, titleStyle);
           else
               group.groupDescription = GUI.TextArea(descriptionBody, group.groupDescription, titleStyle);

           

            GUI.EndGroup();
           GUI.BeginGroup(commentsArea, new GUIContent(""), border);
           Rect commentTitle = new Rect(10, 0, descriptionArea.width - 20, 30);
           GUI.Label(commentTitle, "Comments", fontStyle);

           Rect commentBody = new Rect(6, 36, descriptionArea.width - 12, -40+group.position.height/2);
            if (!group.editHeader)
               GUI.Label(commentBody,group.groupComment, titleStyle);
           else
               group.groupComment = GUI.TextArea(commentBody,group.groupComment,  titleStyle);

            GUI.EndGroup();
           GUI.color = Color.white;



        }


        GUILayout.BeginArea(commentToggleArea, groupBorderSelected);
        group.showComment = EditorGUI.Toggle(new Rect(Vector2.zero, new Vector2(60, 20)), group.showComment, toggle);
        GUI.Label(new Rect(12, 0, 100, 20), "Toggle Comment Box", titleStyle);
        GUILayout.EndArea();
   


    Rect toggleArea = new Rect(new Vector2(group.position.TopRight().x - 120,
                group.position.TopRight().y + 16),
            new Vector2(100, 18));
        GUILayout.BeginArea(toggleArea, groupBorderSelected);

        group.editHeader = EditorGUI.Toggle(new Rect(Vector2.zero,new Vector2(60,20)), group.editHeader, toggle);
        GUI.Label(new Rect(12, 0, 100, 20), "Toggle Edit Mode", titleStyle);
        GUILayout.EndArea();
    }

    private Rect getCurrentViewport()
    {
        Rect viewPort = new Rect();

        viewPort.x = -settings.offset.x / settings.zoomValue;
        viewPort.y = -settings.offset.y / settings.zoomValue;
        viewPort.size = position.size / settings.zoomValue;

        return viewPort;
    }

    private Rect getCurrentOptionsPanel()
    {
        Rect optionsPanel = new Rect();

        optionsPanel.x = lastCenter.x + (position.width - activeOptionsPanelOffset) / 2f / settings.zoomValue;
        optionsPanel.y = lastCenter.y - position.height / 2 / settings.zoomValue;
        optionsPanel.size = new Vector2(activeOptionsPanelOffset, position.height) / settings.zoomValue;

        return optionsPanel;
    }

    private void associateNodes(Node node1, Node node2)
    {
        if (!node1.associatedNodes.Contains(node2))
        {
            node1.associatedNodes.Add(node2);
        }
        if (!node2.associatedNodes.Contains(node1))
        {
            node2.associatedNodes.Add(node1);
        }
    }

    private void deassociateNodes(Node node1, Node node2)
    {
        if (node1.associatedNodes.Contains(node2))
        {
            node1.associatedNodes.Remove(node2);
        }
        if (node2.associatedNodes.Contains(node1))
        {
            node2.associatedNodes.Remove(node1);
        }
    }

    private void deleteNode(Node node)
    {
        List<Connection> deleteList = new List<Connection>();
        activeNodes.Remove(node);
        foreach (Connection con in activeConnections)
        {
            if (con.fromNode == node || con.toNode == node)
            {
                deleteList.Add(con);
            }
        }
        for (int i = 0; i < deleteList.Count; i++)
        {
            activeConnections.Remove(deleteList[i]);
        }
        deleteList.Clear();
        recordUndo();
    }

    private void deleteSelectedNodes()
    {
        if (selectedNodes.Count == 0)
            return;
        List<Connection> deleteList = new List<Connection>();
        foreach (Node curNode in selectedNodes)
        {
            activeNodes.Remove(curNode);
            foreach (Connection con in activeConnections)
            {
                if (con.fromNode == curNode || con.toNode == curNode)
                {
                    deleteList.Add(con);
                }
            }
        }
        for (int i = 0; i < deleteList.Count; i++)
        {
            activeConnections.Remove(deleteList[i]);
        }
        deleteList.Clear();
        selectedNodes.Clear();
        recordUndo();
    }

    /// <summary>
    /// Does not record undo!
    /// </summary>
    /// <returns></returns>
    private Node[] duplicateSelectedNodes()//does not record undo because returning array would be empty!
    {
        List<Node> newNodes = new List<Node>();
        for (int i = 0; i < selectedNodes.Count; i++)
        {
            Node newNode = new Node(selectedNodes[i]);
            activeNodes.Add(newNode);
            DeselectNode(selectedNodes[i]);
            SelectNode(newNode);
            newNode.position.position += new Vector2(newNode.position.width / 2, newNode.position.height / 2);
            newNodes.Add(newNode);
        }
        return newNodes.ToArray();
    }

    /// <summary>
    /// Does not record undo!
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="offsetX"></param>
    /// <param name="offsetY"></param>
    /// <returns></returns>
    private Node[] duplicateNodes(Node[] nodes, float offsetX, float offsetY)//can not use recordUndo because returning array would be empty!
    {
        List<Node> newNodes = new List<Node>();
        foreach (Node curNode in nodes)
        {
            Node newNode = new Node(curNode);
            activeNodes.Add(newNode);
            DeselectNode(curNode);
            SelectNode(newNode);
            newNode.position.position += new Vector2(offsetX, offsetY);
            newNodes.Add(newNode);
        }
        return newNodes.ToArray();
    }

    private void copyNodesToBuffer()
    {
        if (selectedNodes.Count > 0)
        {
            nodeBuffer = new Node[selectedNodes.Count];
            for (int i = 0; i < nodeBuffer.Length; i++)
            {
                nodeBuffer[i] = selectedNodes[i];
            }
        }
    }

    private void pasteNodesFromBuffer()
    {
        if (nodeBuffer == null)
            return;
        DeselectAllNodes();
        Vector2 topLeftNodePosition = new Vector2();
        if (nodeBuffer.Length > 1)
        {
            for (int i = 1; i < nodeBuffer.Length; i++)
            {
                topLeftNodePosition.Set(Mathf.Min(nodeBuffer[i].position.position.x, nodeBuffer[i - 1].position.position.x),//x
                    Mathf.Min(nodeBuffer[i].position.position.y, nodeBuffer[i - 1].position.position.y));//y
            }
        }
        else if (nodeBuffer.Length > 0)
        {
            topLeftNodePosition = nodeBuffer[0].position.position;
        }
        Vector2 positionOffset = mousePos - topLeftNodePosition;
        for (int i = 0; i < nodeBuffer.Length; i++)
        {
            Node newNode = new Node(nodeBuffer[i]);
            activeNodes.Add(newNode);
            SelectNode(newNode);
            newNode.position.position += positionOffset;
        }
        recordUndo();
    }

    private Connection connectNodes(Node from, Node to, ConnectionType type, bool record)
    {
        Connection con = new Connection();
        bool connected = false;

        for (int i = 0; i < activeConnections.Count; i++)
        {
            if ((activeConnections[i].fromNode == from && activeConnections[i].toNode == to) || (activeConnections[i].fromNode == to && activeConnections[i].toNode == from))
            {
                connected = true;
            }
        }

        if (connected)
        {
            UnityEngine.Debug.Log("Already connected with this node!");
        }
        else
        {
            IO_Point selection = from.output;
            IO_Point target = to.input;

            selection.connections.Add(con);
            target.connections.Add(con);

            con.fromNode = from;
            con.toNode = to;
            con.type = type;
            activeConnections.Add(con);

            from.connections.Add(con);
            to.connections.Add(con);

            selection.connectedNodes.Add(target.parentNode);
            target.connectedNodes.Add(selection.parentNode);

            switch (type)
            {
                case ConnectionType.Association:
                    {
                        associateNodes(from, to);
                    }
                    break;
                case ConnectionType.Inheritance:
                    {
                        if(to.parentNode == null)
                        {
                            to.parentNode = from;
                            if (!from.childNodes.Contains(from))
                            {
                                from.childNodes.Add(to);
                            }
                        }
                        else
                        {
                            if (record)
                            {
                                Debug.Log(to.name + " already has parent! ");
                                deleteConnection(con);
                            }
                        }
                    }
                    break;
            }
        }
        if (record)
        {
            recordUndo();
        }
        return con;
    }
    /// <summary>
    /// object[4] = Node from, Node to, ConnectionType type, bool record
    /// Just pass normal parameters as an object array!
    /// </summary>
    /// <param name="userData"></param>
    private void connectNodes(object userData)
    {
        object[] parameters = (object[])userData;
        connectNodes((Node)parameters[0], (Node)parameters[1], (ConnectionType)parameters[2], (bool)parameters[3]);
    }

    private void deleteTargetConnection()
    {
        deleteConnection(targetConnection);
        recordUndo();
    }

    private void deleteConnection(Connection connection)
    {
        connection.toNode.parentNode = null;
        activeConnections.Remove(connection);
        if (connection.fromNode.output.connections.Contains(connection))
        {
            connection.fromNode.output.connections.Remove(connection);
        }
        if (connection.toNode.input.connections.Contains(connection))
        {
            connection.toNode.input.connections.Remove(connection);
        }
        if (connection.fromNode.childNodes.Contains(connection.toNode))
        {
            connection.fromNode.childNodes.Remove(connection.toNode);
        }
        if (connection.fromNode.output.connectedNodes.Contains(connection.toNode.input.parentNode))
        {
            connection.fromNode.output.connectedNodes.Remove(connection.toNode.input.parentNode);
        }
        if (connection.fromNode.input.connectedNodes.Contains(connection.toNode.output.parentNode))
        {
            connection.fromNode.input.connectedNodes.Remove(connection.toNode.output.parentNode);
        }
        recordUndo();
    }

    private Rect getNodeBoundaries(List<Node> NodeList)
    {
        float minX = Mathf.Infinity;
        float minY = Mathf.Infinity;
        float maxX = Mathf.NegativeInfinity;
        float maxY = Mathf.NegativeInfinity;

        foreach (var node in NodeList)
        {
            if (node.position.x < minX)
            {
                minX = node.position.x;
            }
            if (node.position.y < minY)
            {
                minY = node.position.y;
            }
            if (node.position.x + node.position.width > maxX)
            {
                maxX = node.position.x + node.position.width;
            }
            if (node.position.y + node.position.height > maxY)
            {
                maxY = node.position.y + node.position.height;
            }
        }
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }


    private void AddNodeAtMousePosition()
    {
        Rect spawnPos = new Rect(mousePos.x, mousePos.y, 1f,1f);

        string nodeName = "NewNode";
        int nameIndex = 0;
        for (int i = 0; i < activeNodes.Count; i++)
        {
            if (activeNodes[i].name.Contains("NewNode"))
            {
                nameIndex++;
                nodeName = string.Format("NewNode({0})", nameIndex);
            }
        }
        Node newNode = new Node(spawnPos, nodeName);
        activeNodes.Add(newNode);
        DeselectAllNodes();
        SelectNode(newNode);
        recordUndo();
    }

    private void AddNodeAtPosition(Vector2 _pos, bool deselectOthers)
    {
        Rect spawnPos = new Rect(_pos.x, _pos.y, 1f,1f);

        string nodeName = "NewNode";
        int nameIndex = 0;
        for (int i = 0; i < activeNodes.Count; i++)
        {
            if (activeNodes[i].name.Contains("NewNode"))
            {
                nameIndex++;
                nodeName = string.Format("NewNode({0})", nameIndex);
            }
        }
        Node newNode = new Node(spawnPos, nodeName);
        activeNodes.Add(newNode);
        if (deselectOthers)
        {
            DeselectAllNodes();
        }
        SelectNode(newNode);
        recordUndo();
    }

    private void AddNodeAndConnect()
    {
        Rect spawnPos = new Rect(mousePos.x, mousePos.y, 1f,1f);

        string nodeName = "NewNode";
        int nameIndex = 0;
        for (int i = 0; i < activeNodes.Count; i++)
        {
            if (activeNodes[i].name.Contains("NewNode"))
            {
                nameIndex++;
                nodeName = string.Format("NewNode({0})", nameIndex);
            }
        }
        Node newNode = new Node(spawnPos, nodeName);
        activeNodes.Add(newNode);
        DeselectAllNodes();
        SelectNode(newNode);

        if (tempSelectionPoint.parentNode.input == tempSelectionPoint)
        {
            newNode.position.position -= new Vector2(newNode.position.size.x, newNode.position.size.y / 2);
            connectNodes(newNode, tempSelectionPoint.parentNode, ConnectionType.Inheritance, true);
        }
        else
        {
            newNode.position.position -= new Vector2(0, newNode.position.size.y / 2);
            connectNodes(tempSelectionPoint.parentNode, newNode, ConnectionType.Inheritance, true);
        }

        tempSelectionPoint = null;
    }

    /// <summary>
    /// Select node and record undo. Do this when you are selection a single node. For multiple nodes call other overload.
    /// </summary>
    /// <param name="node"></param>
    private void SelectNode(Node node)
    {
        if (!selectedNodes.Contains(node))
        {
            selectedNodes.Add(node);
        }
        node.selected = true;
        targetNode = node;//Last selected node is target node
    }

    private void SelectNodes(Node[] nodes)
    {
        DeselectAllNodes();
        for (int i = 0; i < nodes.Length; i++)
        {
            SelectNode(nodes[i]);
        }
    }

    private void SelectAllNodes(Node node)
    {
        for (int i = 0; i < activeNodes.Count; i++)
        {
            SelectNode(activeNodes[i]);
        }
    }

    private void DeselectNode(Node node)
    {
        if (selectedNodes.Contains(node))
        {
            node.selected = false;
            selectedNodes.Remove(node);
        }
    }

    private void DeselectAllNodes()
    {
        for (int i = 0; i < selectedNodes.Count; i++)//deselect all nodes
        {
            selectedNodes[i].selected = false;
            selectedNodes[i].editText = false;
        }
        selectedNodes.Clear();
    }

    private void DeleteSelectedNodes()
    {
        for (int i = 0; i < selectedNodes.Count; i++)
        {
            for (int j = 0; j < activeGroups.Count; j++)
            {
                if (activeGroups[j].childNodes.Contains(selectedNodes[i]))
                {
                    activeGroups[j].childNodes.Remove(selectedNodes[i]);
                    selectedNodes[i].selected = false;
                    activeNodes.Remove(selectedNodes[i]);
                }
            }
        }
    }

    private void ContextCopySelectedNodes()
    {
        duplicateSelectedNodes();
    }
    #endregion

    #region Grid/Panels

    private void centerScreenToPosition(Vector2 newPos)
    {
        settings.offset = new Vector2(position.width - activeOptionsPanelOffset, position.height) / 2 - newPos * settings.zoomValue;
    }

    private void DrawGrid(float gridSpacing, Color gridColor, Rect rect, SettingsData settings)
    {
        Rect viewport = getCurrentViewport();
        bool updatePoints = false;
        if (lastGridSpacing != gridSpacing ||
            last_XPanelSize != settings.xGraphPanelSize ||
            last_YPanelSize != settings.yGraphPanelSize ||
            settings.zoomValue != lastZoomvalue ||
            settings.offset.x != lastXoffset ||
            settings.offset.y != lastYoffset ||
            gridPoints.Count == 0)
        {
            updatePoints = true;
            gridPoints.Clear();//If points are changed, recreate list
        }

        int widthDivs = Mathf.CeilToInt(rect.width / gridSpacing) + 1;
        int heightDivs = Mathf.CeilToInt(rect.height / gridSpacing) + 1;

        //Draw grid properly
        Handles.color = gridColor;
        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(rect.position + new Vector2(i * gridSpacing, 0f), rect.position + new Vector2(i * gridSpacing, rect.height));
            for (int j = 0; j < heightDivs; j++)
            {
                if (i == widthDivs - 1)
                    Handles.DrawLine(rect.position + new Vector2(0f, j * gridSpacing), rect.position + new Vector2(rect.width, j * gridSpacing));
                if (updatePoints)
                {
                    Vector2 curPos = new Vector2(rect.position.x + i * gridSpacing, rect.position.y + j * gridSpacing);
                    if (viewport.Contains(curPos))
                    {
                        gridPoints.Add(curPos);
                    }
                }
            }
        }

        for (int i = 0; i < widthDivs; i+=10)
        {

            Handles.color = Color.black;
            Vector2 pointA = rect.position + new Vector2(i * gridSpacing, 0f);
            Vector2 pointB = rect.position + new Vector2(i * gridSpacing, rect.height);
            Handles.DrawAAPolyLine(settings.nodeConnectionTexture, 2.5f,
                new[]
                {
                    new Vector3(pointA.x,pointA.y),
                    new Vector3(pointB.x,pointB.y)
                });
            Handles.DrawLine(new Vector3(pointA.x, pointA.y),
                new Vector3(pointB.x, pointB.y));

            for (int j = 0; j < heightDivs; j+=10)
            {
                if (i == widthDivs - 1)
                {
                    Handles.color = Color.black;

                    pointA = rect.position + new Vector2(0f, j * gridSpacing);
                    pointB = rect.position + new Vector2(rect.width, j * gridSpacing);

                    Handles.DrawAAPolyLine(settings.nodeConnectionTexture, 2.5f,
                        new[]
                        {
                            new Vector3(pointA.x,pointA.y),
                            new Vector3(pointB.x,pointB.y)
                        });
                    Handles.DrawLine(new Vector3(pointA.x, pointA.y),
                        new Vector3(pointB.x, pointB.y));

                }
                if (updatePoints)
                {
                    Vector2 curPos = new Vector2(rect.position.x + i * gridSpacing, rect.position.y + j * gridSpacing);
                    if (viewport.Contains(curPos))
                    {
                        gridPoints.Add(curPos);
                    }
                }
            }
        }
        //Borders
        Handles.color = Color.red;
        //top left -> top right
        Handles.DrawLine(rect.position, new Vector2(rect.position.x + rect.size.x, rect.position.y));
        //top left -> bottom left
        Handles.DrawLine(rect.position, new Vector2(rect.position.x, rect.position.y + rect.size.y));
        //bottom right -> top right
        Handles.DrawLine(rect.position + rect.size, new Vector2(rect.position.x + rect.size.x, rect.position.y));
        //bottom right -> bottom left
        Handles.DrawLine(rect.position + rect.size, new Vector2(rect.position.x, rect.position.y + rect.size.y));

        last_XPanelSize = settings.xGraphPanelSize;
        last_YPanelSize = settings.yGraphPanelSize;
        lastGridSpacing = gridSpacing;
        lastZoomvalue = settings.zoomValue;
        lastXoffset = settings.offset.x;
        lastYoffset = settings.offset.y;
    }
    #endregion

    #region Class Generation
    private void ContextCreateScripts()
    {
        GenerateScript.Generate(activeGroups,activeNodes,settings.scriptSavePath,settings.makeSerialiazable);
        TERMINATE = true;
    }

    #endregion

    #region Events
    private void HandleEvents(Event current, Vector2 mousePos)
    {

        //optionspanel adjustments
        Rect optionsPanelResizeControl = new Rect(position.width - activeOptionsPanelOffset - 2f, 0, 4f, position.height);
        EditorGUIUtility.AddCursorRect(optionsPanelResizeControl, MouseCursor.ResizeHorizontal);

        //Left click selects only one node...
        Rect visibleGraphPanel = new Rect(graphPanel);
        visibleGraphPanel.size *= settings.zoomValue;
        //optionsPanel.position = (optionsPanel.position - settings.offset) / settings.zoomValue;
        //optionsPanel.size = optionsPanel.size / settings.zoomValue;
        Rect optionsPanelControlRect = getCurrentOptionsPanel();

        if (current.type == EventType.MouseDown && current.button == 0 && optionsPanelResizeControl.Contains(current.mousePosition))
        {
            resizeMode = true;
            return;
        }

        if (current.type == EventType.KeyDown&&current.keyCode==KeyCode.Space)
            foreach (var node in activeNodes)
                if (node.position.Contains(mousePos))
                    SelectNode(node);
        if (current.type == EventType.MouseDown && current.button == 0 && graphPanel.Contains(mousePos) && !optionsPanelControlRect.Contains(mousePos))//left mouse click inside panel
        {
            clickedOnNode = false;

            if (targetConnection != null)
            {
                releasingConnection = true;
                goto BreakMouseDown;//No need to calculate rest
            }
            else
            {
                releasingConnection = false;
            }

            for (int i = visibleNodes.Count - 1; i >= 0; i--)
            {
                if (Vector2.Distance(visibleNodes[i].input.position.center, mousePos) < settings.connectionPointRadius)
                {
                    selectedPoint = visibleNodes[i].input;
                    break;
                }
                else
                {
                    selectedPoint = null;
                }
                if (Vector2.Distance(visibleNodes[i].output.position.center, mousePos) < settings.connectionPointRadius)
                {
                    selectedPoint = visibleNodes[i].output;
                    break;
                }
                else
                {
                    selectedPoint = null;
                }
                if (visibleNodes[i].position.Contains(mousePos))
                {
                    if (!current.control)
                    {
                        DeselectAllNodes();
                    }
                    if (!selectedNodes.Contains(visibleNodes[i]))//Clicked on a non selected node
                    {
                        SelectNode(visibleNodes[i]);
                        activeNodes.Swap(activeNodes.IndexOf(visibleNodes[i]), activeNodes.Count - 1);
                    }
                    else//Clicked on a selected node
                    {
                        targetNode = visibleNodes[i];
                        markedForDeselect = visibleNodes[i];
                    }

                    clickedOnNode = true;
                    break;
                }
            }

            dragging = false;
            if (clickedOnNode == false)//not clicked on a node
            {
                if (!dragging)
                {
                    DeselectAllNodes();
                }
                closestNode = null;
                clickedOnNode = false;
                selectionRect.Set(0f, 0f, 0f, 0f);
                GUI.FocusControl(null);

                for (int i = 0; i < activeGroups.Count; i++)
                {
                    if (activeGroups[i].position.Contains(mousePos))//clicked in a group
                    {
                        targetGroup = activeGroups[i];
                        break;
                    }
                }
            }
        }
    BreakMouseDown:

        if (current.type == EventType.ContextClick && graphPanel.Contains(mousePos) && !optionsPanelControlRect.Contains(mousePos))
        {

            if (targetConnection != null)
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Delete Connection"), false, deleteTargetConnection);

                menu.ShowAsContext();

                current.Use();
                goto BreakContext;
            }

            Node selectedNode = null;
            for (int i = 0; i < visibleNodes.Count; i++)
            {
                if (visibleNodes[i].position.Contains(mousePos))
                {
                    selectedNode = visibleNodes[i];
                }
            }

            Group selectedGroup = null;
            for (int i = 0; i < activeGroups.Count; i++)
            {
                if (activeGroups[i].position.Contains(mousePos))
                {
                    selectedGroup = activeGroups[i];
                }
            }

            if (selectedNode == null)
            {
                if (selectedGroup == null)
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Add New Node"), false, AddNodeAtMousePosition);
                    //menu.AddItem(new GUIContent("Take Screenshot"), false, TakeScreenshot);
                    menu.ShowAsContext();

                    current.Use();
                }
                else
                {
                    targetGroup = selectedGroup;

                    GenericMenu menu = new GenericMenu();

                    //menu.AddItem(new GUIContent("Take Screenshot of Group"), false, TakeGroupScreenshot,selectedGroup);
                    menu.AddItem(new GUIContent("Duplicate group"), false, ContextDuplicateGroup);
                    menu.AddItem(new GUIContent("Delete group"), false, ContextDeleteGroup);

                    menu.ShowAsContext();
                    current.Use();
                }
            }
            else//Clicked on a node
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Duplicate Node(s)"), false, ContextCopySelectedNodes);

        

                if (selectedNodes.Count > 1)
                {
                    menu.AddItem(new GUIContent("Create Card from selected nodes"), false, ContextCreateGroup);
                }
                bool hasGroup = true;
                Group curGroup = null;
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    if (curGroup != null && selectedNodes[i].parentGroup != curGroup)
                    {
                        hasGroup = false;
                        break;
                    }
                    if (selectedNodes[i].parentGroup == null)
                    {
                        hasGroup = false;
                        break;
                    }
                    else
                    {
                        curGroup = selectedNodes[i].parentGroup;
                    }
                }
                if (hasGroup)
                {
                    menu.AddItem(new GUIContent("Remove nodes from group"), false, ContextRemoveNodesFromGroup);
                }

                menu.ShowAsContext();

                current.Use();
            }

        }

    BreakContext:

        if (current.type == EventType.MouseMove)
        {
            drawSelectionRect = false;
            for (int i = 0; i < visibleNodes.Count; i++)
            {
                Vector2 inputPosition = visibleNodes[i].input.position.center;
                Vector2 outputPosition = visibleNodes[i].output.position.center;

                if (Vector2.Distance(inputPosition, mousePos) < settings.connectionPointRadius)//Hovering on input position
                {
                    visibleNodes[i].input.hover = true;
                    if (targetConnection != null)
                    {
                        targetConnection.hover = false;
                        targetConnection = null;
                    }
                    goto BreakMouseMove;
                }
                else
                {
                    visibleNodes[i].input.hover = false;
                }
                if (Vector2.Distance(outputPosition, mousePos) < settings.connectionPointRadius)//Hovering on output position
                {
                    visibleNodes[i].output.hover = true;
                    if (targetConnection != null)
                    {
                        targetConnection.hover = false;
                        targetConnection = null;
                    }
                    goto BreakMouseMove;
                }
                else
                {
                    visibleNodes[i].output.hover = false;
                }
            }

            targetConnection = null;

            for (int i = 0; i < activeConnections.Count; i++)
            {
                for (int j = 0; j < activeConnections[i].lineArray.Length -1; j++)
                {
                    activeConnections[i].hover = false;
                    if (!activeConnections[i].controlRect.Contains(mousePos))
                    {
                        continue;
                    }
                    if(HandleUtility.DistancePointLine(mousePos, activeConnections[i].lineArray[j], activeConnections[i].lineArray[j + 1]) < 14f)
                    {
                        activeConnections[i].hover = true;
                        targetConnection = activeConnections[i];
                        activeConnections.Swap(targetConnection.ID, activeConnections.Count - 1);
                        break;
                    }
                }
            }
        }

    BreakMouseMove:

        if (current.control && current.type == EventType.KeyUp)// Control + KeyCode commands here
        {
            if (current.keyCode == KeyCode.C)
            {
                copyNodesToBuffer();
            }
            if (current.keyCode == KeyCode.V)
            {
                pasteNodesFromBuffer();
            }
            if (current.keyCode == KeyCode.D)//Duplicate selected nodes
            {
                duplicateSelectedNodes();
                recordUndo();
            }
            if (current.keyCode == KeyCode.Z)
            {
                Undo();
            }
            if (current.keyCode == KeyCode.Y)
            {
                Redo();
            }
        }

        if (current.type == EventType.KeyDown)//Keyboard controls here
        {
            if (current.keyCode == KeyCode.Delete)
            {
                deleteSelectedNodes();
            }
            if (current.keyCode == KeyCode.F)//Reset zoom
            {
                setZoom(1f);
                if (selectedNodes.Count > 0)
                {
                    Rect nodeBoundaries = getNodeBoundaries(selectedNodes);
                    Vector2 panelSize = new Vector2(position.width - activeOptionsPanelOffset, position.height);

                    if (nodeBoundaries.width / panelSize.x > nodeBoundaries.height / panelSize.y)//fit according to width
                    {
                        setZoom(Mathf.Clamp(panelSize.x / nodeBoundaries.width * .9f, minZoomValue, 1.3f));
                    }
                    else
                    {
                        setZoom(Mathf.Clamp(panelSize.y / nodeBoundaries.height * .9f, minZoomValue, 1.3f));
                    }
                    centerScreenToPosition(nodeBoundaries.center);
                }
            }
            if (current.keyCode == KeyCode.A)//Select all nodes
            {
                if (selectedNodes.Count > 0)
                {
                    DeselectAllNodes();
                }
                else
                {
                    for (int i = 0; i < activeNodes.Count; i++)
                    {
                        SelectNode(activeNodes[i]);
                    }
                }
            }
        }

        if (current.type == EventType.MouseDrag)//Mouse is dragging
        {
            if (!dragging)//Started dragging this frame
            {
                dragging = true;
                mouseDragStartPosition = mousePos;
                if (current.button == 0)
                {
                    for (int i = 0; i < selectedNodes.Count; i++)//Check if mouse is inside a node
                    {
                        if (selectedNodes[i].position.Contains(mousePos))
                        {
                            clickedOnNode = true;
                        }
                    }
                }
            }

            if (current.button == 0)//Left mouse drag
            {
                if (releasingConnection && Vector2.Distance(mouseDragStartPosition, mousePos) > 15f)
                {
                    releasingConnection = false;
                    if (Vector2.Distance(targetConnection.fromNode.output.position.position, mousePos) < Vector2.Distance(targetConnection.toNode.input.position.position, mousePos))
                    {
                        selectedPoint = targetConnection.toNode.input;//if selectedPoint is not null, we will enter create connection mode (a.k.a. drawLines) See below
                    }
                    else
                    {
                        selectedPoint = targetConnection.fromNode.output;
                    }

                    deleteTargetConnection();
                }
                //clickedOnNode = false;
                if (resizeMode)
                {
                    settings.optionsPanelOffset -= current.delta.x;
                    return;
                }
                float minNodeDistance = Mathf.Infinity;
                for (int i = 0; i < selectedNodes.Count; i++)//Check if mouse is inside a node
                {
                    if (Vector2.Distance(selectedNodes[i].position.position, mousePos) < minNodeDistance)
                    {
                        minNodeDistance = Vector2.Distance(selectedNodes[i].position.position, mousePos);
                        closestNode = selectedNodes[i];
                    }
                }
                if (selectedPoint != null)//if there is a selected node, draw lines(enter "create connection mode")
                {
                    drawSelectionRect = false;
                    drawLines = true;
                }
                else
                {
                    drawLines = false;
                }
                if (!drawLines)//If not drawing a line from a node to another
                {
                    if (!releasingConnection)
                    {
                        selectionRect.Set(Mathf.Min(mousePos.x, mouseDragStartPosition.x),
                            Mathf.Min(mousePos.y, mouseDragStartPosition.y),
                            Mathf.Abs(mousePos.x - mouseDragStartPosition.x),
                            Mathf.Abs(mousePos.y - mouseDragStartPosition.y));
                    }

                    if (graphPanel.Contains(mousePos) && !optionsPanelControlRect.Contains(mousePos))
                    {
                        if (current.shift)//Snapping
                        {
                            if (closestNode != null)
                            {
                                for (int j = 0; j < gridPoints.Count; j++)
                                {
                                    if (Vector2.Distance(mousePos, gridPoints[j]) < settings.gridSpacing)
                                    {
                                        closestGridPoint = gridPoints[j];
                                        break;
                                    }
                                }

                                Vector2 snapOffset = closestGridPoint - closestNode.position.position;

                                if (gridPoints.Count == 0)
                                {
                                    snapOffset = current.delta / settings.zoomValue;
                                }

                                for (int i = 0; i < selectedNodes.Count; i++)
                                {
                                    selectedNodes[i].position.position += snapOffset;

                                }
                            }

                            if (targetGroup != null)
                            {
                                for (int j = 0; j < gridPoints.Count; j++)
                                {
                                    if (Vector2.Distance(mousePos, gridPoints[j]) < settings.gridSpacing)
                                    {
                                        closestGridPoint = gridPoints[j];
                                        break;
                                    }
                                }

                                Vector2 snapOffset = closestGridPoint - targetGroup.position.position;

                                for (int i = 0; i < targetGroup.childNodes.Count; i++)
                                {
                                    targetGroup.childNodes[i].position.position += snapOffset;
                                }
                            }
                        }
                        else
                        {
                            if (clickedOnNode)
                            {
                                drawSelectionRect = false;
                                bool noNodesHaveGroup = true;
                                for (int i = 0; i < selectedNodes.Count; i++)
                                {
                                    selectedNodes[i].position.position += current.delta / settings.zoomValue;
                                    if (selectedNodes[i].parentGroup != null)
                                    {
                                        noNodesHaveGroup = false;
                                    }
                                }
                                if (noNodesHaveGroup)//If none of the selected nodes are attached to a group
                                {
                                    parentTargetGroup = null;
                                    for (int i = 0; i < activeGroups.Count; i++)
                                    {
                                        if (activeGroups[i].position.Contains(mousePos))
                                        {
                                            parentTargetGroup = activeGroups[i];
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (targetGroup != null)
                                {
                                    for (int i = 0; i < targetGroup.childNodes.Count; i++)
                                    {
                                        targetGroup.childNodes[i].position.position += current.delta / settings.zoomValue;
                                    }
                                }
                                else
                                {
                                    drawSelectionRect = true;
                                    for (int i = 0; i < visibleNodes.Count; i++)
                                    {
                                        if (selectionRect.Contains(visibleNodes[i].position.center) ||
                                            selectionRect.Contains(visibleNodes[i].position.position) ||
                                            selectionRect.Contains(visibleNodes[i].position.position + visibleNodes[i].position.size) ||
                                            selectionRect.Contains(visibleNodes[i].position.position + new Vector2(visibleNodes[i].position.width, 0f)) ||
                                            selectionRect.Contains(visibleNodes[i].position.position + new Vector2(0f, visibleNodes[i].position.height))
                                            )
                                        { 
                                            SelectNode(visibleNodes[i]);
                                        }
                                        else
                                        {
                                            DeselectNode(visibleNodes[i]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (current.button == 2)//Middle mouse drag
            {
                if (current.control == true)
                {
                    increaseZoom(-current.delta.y * 0.005f);
                }
                else
                {
                    settings.offset += current.delta;
                }
            }
        }

        if (current.type == EventType.MouseUp && current.button == 0)//Left mouse up
        {
            if (selectedPoint != null)
            {
                bool targetIsInput = false;
                IO_Point target = null;
                for (int i = 0; i < visibleNodes.Count; i++)
                {
                    if (Vector2.Distance(visibleNodes[i].input.position.center, mousePos) < settings.connectionPointRadius)
                    {
                        targetIsInput = true;
                        target = visibleNodes[i].input;
                    }
                    if (Vector2.Distance(visibleNodes[i].output.position.center, mousePos) < settings.connectionPointRadius)
                    {
                        target = visibleNodes[i].output;
                    }
                }
                if (target != null && target.parentNode != selectedPoint.parentNode)//there is target to connect -- Connect Nodes
                {
                    GenericMenu menu = new GenericMenu();

                    if (selectedPoint == selectedPoint.parentNode.input)//selection is input
                    {
                        if (!targetIsInput)//target is output
                        {
                            object[] data = { target.parentNode, selectedPoint.parentNode, ConnectionType.Association, true };
                            menu.AddItem(new GUIContent("Associate"), false, connectNodes, data);
                            data = new object[] { target.parentNode, selectedPoint.parentNode, ConnectionType.Inheritance, true };
                            menu.AddItem(new GUIContent("Inherit"), false, connectNodes, data);
                        }
                    }
                    else//selection is output
                    {
                        if (targetIsInput)//target is input
                        {
                            object[] data = { selectedPoint.parentNode, target.parentNode, ConnectionType.Association, true };
                            menu.AddItem(new GUIContent("Associate"), false, connectNodes, data);
                            data = new object[] { selectedPoint.parentNode, target.parentNode, ConnectionType.Inheritance, true };
                            menu.AddItem(new GUIContent("Inherit"), false, connectNodes, data);
                        }
                    }

                    menu.ShowAsContext();

                    current.Use();
                }
                else//No target, open context menu to create new node-- if created connect them
                {
                    tempSelectionPoint = selectedPoint;

                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Create New Node Here"), false, AddNodeAndConnect);
                    menu.ShowAsContext();

                    current.Use();
                }
                selectedPoint = null;
            }
            else
            {
                if (parentTargetGroup != null && selectedNodes.Count > 0)
                {
                    moveNodesToGroup(selectedNodes.ToArray(), parentTargetGroup);
                }

                foreach (Node curNode in selectedNodes)
                {
                    if (!dragging && current.control)
                    {
                        if (curNode.position.Contains(mousePos) && curNode == markedForDeselect)
                        {
                            DeselectNode(curNode);
                            break;
                        }
                    }
                }
                if (!clickedOnNode)
                {
                    for (int i = 0; i < activeGroups.Count; i++)
                    {
                        if (activeGroups[i].position.Contains(mousePos))//clicked in a group
                        {
                            SelectNodes(activeGroups[i].childNodes.ToArray());
                            break;
                        }
                    }
                }
            }

            resizeMode = false;

            drawLines = false;
            if (dragging && clickedOnNode)
            {
                recordUndo();
                dragging = false;
            }
            closestNode = null;
            clickedOnNode = false;
            selectionRect.Set(0f, 0f, 0f, 0f);
            markedForDeselect = null;
            drawSelectionRect = false;
            targetGroup = null;

            parentTargetGroup = null;
            targetConnection = null;
        }

        if (current.type == EventType.ScrollWheel)//Mouse scroll 
        {
            increaseZoom(-current.delta.y / 50f);
        }
    }
    #endregion

    #region Custom Methods
    public static GUIStyle GetStyleByName(string name)
    {
        return customStyles.Where(x => x.name == name).Select(x => x).ElementAt(0);
    }
    #endregion

    protected virtual void OnEnable()
    {

    }

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= OnEditorUpdate;
#endif
    }

    protected virtual void OnEditorUpdate()
    {
        // In here you can check the current realtime, see if a certain
        // amount of time has elapsed, and perform some task.
    }

    #region Built-in Messages(Default EditorWindow Methods)
   
    [MenuItem("Window/Node Editor")]
    static void Init()
    {
        loadSettings();
        Node.settings = settings;
        NodeEditor window = (NodeEditor)GetWindow(typeof(NodeEditor));
        window.Show();

        window.position = new Rect(settings.windowPosition, settings.windowSize);

        settings.offset.Set(-(settings.xGraphPanelSize / 2 - (window.position.width - activeOptionsPanelOffset) / 2), -(settings.yGraphPanelSize / 2 - window.position.height / 2));

        texturePack = new TexturePack();
        texturePack.Load();

        globalFont = (Font)AssetDatabase.LoadAssetAtPath(fontPath, typeof(Font));
        activeOptionsPanelOffset = settings.optionsPanelOffset;

        //Init styles
        customStyles = settings.styleContainer.customStyles.ToArray();
        Node.Init(customStyles);

        int startingIndex = Array.IndexOf(customStyles, GetStyleByName("node_gray_off"));
        for (int i = 0; i < settings.presetColors.Length; i++)
        {
            settings.presetColors[i] = new NodeColor(settings.presetColors[i].name, startingIndex + 1, startingIndex);
            startingIndex += 2;
        }

        buttonStyle = GetStyleByName("pd_button");

        hintsStyle = GetStyleByName("pd_hints");
        hintsStyle.wordWrap = true;

        fontStyle = GetStyleByName("node_gray_off");
        fontStyle.font = EditorStyles.whiteMiniLabel.font;
        fontStyle.fontStyle = FontStyle.Bold;
        fontStyle.fontSize = 21;
        fontStyle.margin = new RectOffset(0, 0, 0, 10);
        fontStyle.contentOffset = new Vector2(0, -14);
        fontStyle.alignment = TextAnchor.MiddleCenter;

        groupBorder = GetStyleByName("pd_group_border");
        groupBorderSelected = GetStyleByName("pd_group_border_selected");

        toggle = GetStyleByName("pd_toggle");
        //Init styles end

    }

    public void Awake()
    {
        recordUndo();//Make first record with blank page

    }

    void Update()
    {

        if (screenshotMode)//Requested a screenshot, screenshot operations will be handled here. Then automatically end screenshot mode.
        {
            screenshotTimer += 0.01f;

            if (screenshotTimer < settings.screenshotSensitivity)
                return;
            screenshotTimer = 0f;

            Texture2D curTexture = null;

            int texture_x_size = Mathf.CeilToInt(position.width);
            int texture_y_size = Mathf.CeilToInt(position.height);
            float yDif = 0f;
            if(-settings.offset.x + texture_x_size > screenshotBorder.width)
            {
                texture_x_size = Mathf.CeilToInt(screenshotBorder.width + settings.offset.x);
            }
            if(-settings.offset.y + texture_y_size > screenshotBorder.height)
            {
                yDif = position.height - (screenshotBorder.height + settings.offset.y);
                texture_y_size = Mathf.CeilToInt(screenshotBorder.height + settings.offset.y);
            }
            curTexture = new Texture2D(texture_x_size, texture_y_size, TextureFormat.RGB24, false);
            curTexture.ReadPixels(new Rect(0, yDif, texture_x_size, texture_y_size), 0, 0);
            curTexture.Apply();
            screenshot.SetPixels(Mathf.CeilToInt(-settings.offset.x), Mathf.CeilToInt(screenshotBorder.size.y + settings.offset.y) - curTexture.height, curTexture.width, curTexture.height, curTexture.GetPixels());
            UnityEngine.Debug.Log("Offset:" + -settings.offset + " xSize :" + texture_x_size + " ySize: " + texture_y_size);

            settings.offset.x -= position.width;
            if (settings.offset.x <= -screenshotBorder.size.x)
            {
                settings.offset.y -= position.height;
                settings.offset.x = 0;
            }
            curTake++;
            Debug.Log("Cur Take : " + curTake + " Total Takes : " + verticalTakes * horizontalTakes);
            if (curTake == verticalTakes * horizontalTakes)
            {
                screenshot.Apply();
                byte[] bytes = screenshot.EncodeToPNG();
                if (Directory.Exists(settings.screenshotSavePath))
                {
                    string curPath = settings.screenshotSavePath + "/" + settings.screenshotName + ".png";
                    int id = 1;
                    while (File.Exists(curPath))
                    {
                        curPath = settings.screenshotSavePath + "/" + settings.screenshotName + "(" + id + ")" + ".png";
                        id++;
                    }
                    File.WriteAllBytes(curPath, bytes);

                }
                else
                {
                    File.WriteAllBytes(string.Format("Assets/Screenshot" + "({0})" + ".png", curTake), bytes);
                }

                settings.zoomValue = oldZoomValue;
                settings.offset = oldOffset;
                screenshotMode = false;
            }
        }
    }
    void OnGUI()
    {
        string[] customClasses = new string[activeNodes.Count];
        for (int i = 0; i < activeNodes.Count; i++)
            customClasses[i] = activeNodes[i].name;

        if (TERMINATE == true)
            Close();
        if (recordCurrentState)
        {
            recordUndo();
            recordCurrentState = false;
        }
        //init values++
        if (globalSkinScene == null)
            globalSkinScene = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);
        if (globalSkinInspector == null)
            globalSkinInspector = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
        GUI.skin.font = globalFont;

        minZoomValue = Mathf.Max((position.width - activeOptionsPanelOffset) / settings.xGraphPanelSize, position.height / settings.yGraphPanelSize);
        settings.zoomValue = Mathf.Clamp(settings.zoomValue, minZoomValue, maxZoomValue);
        settings.optionsPanelOffset = Mathf.Clamp(settings.optionsPanelOffset, 100f, position.width / 2);

        wantsMouseMove = true;

        Event current = Event.current;
        mousePos = (current.mousePosition - settings.offset) / settings.zoomValue;

        optionsPanel = new Rect(position.width - activeOptionsPanelOffset, 0, activeOptionsPanelOffset, position.height);
        //init values--

        #region Graph Panel
        //-----------------

        lastCenter = new Vector2(position.width - activeOptionsPanelOffset, position.height);
        lastCenter = lastCenter / 2;
        lastCenter = (lastCenter - settings.offset) / settings.zoomValue;

        if (!screenshotMode)
        {
            xOffset = settings.xGraphPanelSize * settings.zoomValue - position.width + activeOptionsPanelOffset;
            yOffset = settings.yGraphPanelSize * settings.zoomValue - position.height;

            settings.offset.x = Mathf.Clamp(settings.offset.x, -xOffset, 0);//Clamp offset value too, it will not exceed limits
            settings.offset.y = Mathf.Clamp(settings.offset.y, -yOffset, 0);
        }

        graphPanel = new Rect(0, 0, settings.xGraphPanelSize, settings.yGraphPanelSize);

        EditorZoomArea.Begin(settings.zoomValue, graphPanel, new Rect(0, 0, position.width, position.height), settings.offset, mousePos);
        EditorZoomArea.screenshotMode = screenshotMode;

        GUI.DrawTexture(graphPanel, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, settings.graphPanelColor, Vector4.zero, 0f);
        if (settings.showGrid)
        {
            DrawGrid(settings.gridSpacing, settings.gridLineColor, graphPanel, settings);
        }
        else
        {
            gridPoints.Clear();
        }
        #region SnapSettings-Furkan
        if (dragging && selectedNodes != null)
        {

            if (selectedNodes.Count > 0 && Event.current.shift)
            {
                Handles.color = Color.yellow;
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    Node selected = selectedNodes[i];
                    foreach (Node node in activeNodes)
                    {

                        if (node != selected)
                        {
                            if ((int)node.position.x == (int)selected.position.x)
                            {
                                if (node.position.y > selected.position.y)
                                {
                                    Handles.DrawLine(
                                        new Vector3(node.position.x, node.position.y, -1),
                                        new Vector3(selected.position.x, selected.position.y + selected.position.height,
                                            -1));
                                }
                                else
                                {
                                    Handles.DrawLine(
                                        new Vector3(node.position.x, node.position.y + node.position.height, -1),
                                        new Vector3(selected.position.x, selected.position.y,
                                            -1));
                                }


                            }

                            if ((int)node.position.y == (int)selected.position.y)
                            {
                                if (node.position.x > selected.position.x)
                                {
                                    Handles.DrawLine(
                                        new Vector3(node.position.x, node.position.y, -1),
                                        new Vector3(selected.position.x + selected.position.width, selected.position.y,
                                            -1));
                                }
                                else
                                {
                                    Handles.DrawLine(
                                        new Vector3(node.position.x + node.position.width, node.position.y, -1),
                                        new Vector3(selected.position.x, selected.position.y,
                                            -1));
                                }
                            }

                        }


                    }
                }
            }
        }
        #endregion
        //Draw grid inside the graph panel


        //Call DrawGroup for each active card
        for (int i = 0; i < activeGroups.Count; i++)
        {
            DrawGroup(activeGroups[i]);
        }

        if (drawLines)
        {
            float bezierPointOffset = Mathf.Clamp(Mathf.Abs(selectedPoint.position.center.x - mousePos.x) * 0.6f, 25f, 250f);
            if (selectedPoint.parentNode.input == selectedPoint)//selected is input point
            {
                Vector2 start = selectedPoint.position.center;
                Vector2 end = mousePos;
                Vector2 startTangent = selectedPoint.position.center + Vector2.right * -bezierPointOffset;
                Vector2 endTangent = mousePos + Vector2.left * -bezierPointOffset;

                DrawingUtilities.DrawLineArray(Handles.MakeBezierPoints(start, end, startTangent, endTangent,40),Color.yellow);
            }
            else
            {
                Vector2 start = selectedPoint.position.center;
                Vector2 end = mousePos;
                Vector2 startTangent = selectedPoint.position.center + Vector2.right * bezierPointOffset;
                Vector2 endTangent = mousePos + Vector2.left * bezierPointOffset;
                DrawingUtilities.DrawLineArray(Handles.MakeBezierPoints(start, end, startTangent, endTangent, 40), Color.yellow);
            }
        }

        #region Draw Connections
        //Draw connections
        for (int i = 0; i < activeConnections.Count; i++)
            activeConnections[i].Draw(settings);
        #endregion

        #region Draw Nodes
        //Draw Nodes
        bool delete = false;
        List<Node> del_list = new List<Node>();
        visibleNodes.Clear();
        Rect viewPort = getCurrentViewport();



        foreach (Node curNode in activeNodes)
        {
            if (viewPort.Contains(curNode.position.position) ||
                viewPort.Contains(curNode.position.BottomRight()) ||
                viewPort.Contains(curNode.position.BottomLeft()) ||
                viewPort.Contains(curNode.position.TopRight()) ||
                viewPort.Contains(curNode.position.center))
            {
                if (curNode.Draw_Node(graphPanel, customClasses, curNode == targetNode, texturePack,  out delete, globalFont, globalSkinScene))
                {
                    recordCurrentState = true;
                }
                if (delete)
                {
                    del_list.Add(curNode);
                }
                if (curNode.position.Contains(mousePos))
                    curNode.hover = true;
                else
                    curNode.hover = false;
                visibleNodes.Add(curNode);
            }
            else
            {
                curNode.CreateIOPositions(5);//If node is not visible still create IO_Points to keep connections
            }
        }
        for (int i = 0; i < del_list.Count; i++)
        {
            deleteNode(del_list[i]);
        }
        #endregion

        EditorZoomArea.End();

        //Draw selection rect
        if (drawSelectionRect && selectionRect.size.magnitude > 1f)
        {
            Rect newRect = new Rect(selectionRect.position * settings.zoomValue + settings.offset, selectionRect.size * settings.zoomValue);
            GUI.DrawTexture(newRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0.8f, 0.8f, 0.1f), 0f, 0f);
            GUI.DrawTexture(newRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0.8f, 0.8f, 0.8f), 1f, 0f);
        }

        //------------------
        #endregion

        #region Options Panel
        //-------------------

        if ((!settings.optionsPanelEnabled && activeOptionsPanelOffset < 1f)|| screenshotMode)
        {
            goto OptionsPanelEnd;
        }
        GUILayout.BeginArea(optionsPanel);

        Rect optionsBackground = new Rect(0, 0, position.width, position.height);
        GUI.DrawTexture(optionsBackground, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, settings.optionsPanelColor, 0f, 0f);
        GUI.DrawTexture(optionsBackground, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.black, 1f, 0f);

        GUILayout.BeginArea(new Rect(5f, 5f, optionsPanel.width - 10f, optionsPanel.height - 10f));
        optionsScrollPosition = GUILayout.BeginScrollView(optionsScrollPosition);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            wordWrap = true,
            font = globalFont,
            fontSize = 11
        };
        GUILayout.Label(
            "DISCLAIMER: This is the beta version of the ProjectDesigner+.", titleStyle);

        GUILayout.BeginHorizontal();
        float width = 128;
        GUILayout.Space((activeOptionsPanelOffset / 2) - width / 2);
        GUILayout.Label(settings.birchgamesTexture, GUILayout.MaxWidth(width), GUILayout.MaxHeight(width));
        GUILayout.EndHorizontal();

        settings.optionsPanelColor = EditorGUILayout.ColorField("Options Panel Color", settings.optionsPanelColor);
        settings.graphPanelColor = EditorGUILayout.ColorField("Graph Panel Color", settings.graphPanelColor);

        settings.styleContainer = EditorGUILayout.ObjectField("Style Container", settings.styleContainer, typeof(StyleContainer), false) as StyleContainer;

        globalFont = EditorGUILayout.ObjectField("Font:", globalFont, typeof(Font), false) as Font;

        EditorGUILayout.Separator();

        #region Save & Load
        showSaveLoad = EditorGUILayout.Foldout(showSaveLoad, "Save & Load");
        if (showSaveLoad)
        {
            EditorGUILayout.LabelField("Editor Settings", EditorStyles.boldLabel);
            //Save Settings
            bool _save = GUILayout.Button("Save Settings", buttonStyle);
            if (_save)
            {
                saveSettings();
            }

            //Reset Settings
            bool _reset = GUILayout.Button("Reset Settings to Default", buttonStyle);
            if (_reset)
            {
                resetSettings();
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Content Path", EditorStyles.boldLabel);
            //Save Contents
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(settings.contentSavePath, GUILayout.Width(activeOptionsPanelOffset / 2));

            bool _choosePath = GUILayout.Button("Choose Path", buttonStyle);

            EditorGUILayout.EndHorizontal();

            if (_choosePath)
            {
                string savePath = EditorUtility.OpenFolderPanel("Choose Save Path", settings.contentSavePath, "");
                if(savePath!=null)
                    if (savePath != "")
                    {
                        string[] folders = savePath.Split('/');

                        bool saveNow = false;
                        string @final = "";
                        for (int i = 0; i < folders.Length; i++)
                        {
                            if (folders[i] == "Assets")
                                saveNow = true;
                            if(saveNow)
                                @final += folders[i] + "/";
                        }

                        @final = @final.TrimEnd('/');
                        settings.contentSavePath = @final;

                    }

            }

            settings.contentName = EditorGUILayout.TextField("File Name", settings.contentName, GUILayout.MinWidth(activeOptionsPanelOffset / 2));

            bool _saveContents = GUILayout.Button("Save Contents", buttonStyle);
            if (_saveContents)
            {
                string curPath = checkFileName(settings.contentSavePath + "/" + settings.contentName);
                saveContents(curPath);
            }


            //Load Contents
            loadButton = GUILayout.Button("Load Contents", buttonStyle);
            if (loadButton)
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Content Data to Load", settings.contentSavePath, "asset");
                loadContents(selectedPath);
            }
        }
        #endregion

        #region Hints
        GUILayoutOption hintWidth = GUILayout.MinWidth(activeOptionsPanelOffset-8);
        GUIContent curContent = new GUIContent("Show Hints", EditorGUIUtility.IconContent("_Help").image, "Show user manual");
        showHints = EditorGUILayout.Foldout(showHints, curContent);
        if (showHints)
        {
            //GUILayout.Label("", EditorStyles.helpBox);  <--Template for copy-paste

            GUILayout.Label("To create a new node, you can drag a line from a connection point and drop on an empty space",hintsStyle, hintWidth);
            GUILayout.Label("To change node color, click on class icon(birch tree for default)", hintsStyle, hintWidth);
            GUILayout.Label("There are two types of line: Inherit Line and Association Line", hintsStyle, hintWidth);
            GUILayout.Label("Click on check mark to toggle edit mode for nodes", hintsStyle, hintWidth);
            GUILayout.Label("You can remove a node by clicking on the X that is on the right-top corner of the node", hintsStyle, hintWidth);
            GUILayout.Label("To generate scripts, right click on a node and press 'Create Class'", hintsStyle, hintWidth);
            GUILayout.Label("If you leave method and field names blank or default, it will throw compile error", hintsStyle, hintWidth);
            GUILayout.Label("There are two different line styles: Curved and Straight ", hintsStyle, hintWidth);
            GUILayout.Label("You can remove a connection by right clicking on a connection and pressing on 'Delete Connection'", hintsStyle, hintWidth);
            GUILayout.Label("Derived node will also take node color of the base class", hintsStyle, hintWidth);
            GUILayout.Label("To create a new card select multiple nodes and press on 'Create card from selected nodes. This will create a card(group) which will be used" +
                            "in script generation" , hintsStyle, hintWidth);


            GUILayout.Label("Reset Zoom : F", hintsStyle, hintWidth);
            GUILayout.Label("Select-Deselect All Nodes : A", hintsStyle, hintWidth);

            GUILayout.Label("Multi-Select : Ctrl + Left Click ", hintsStyle, hintWidth);
            GUILayout.Label("Selection Box : Left Click + Mouse Drag (On empty space)", hintsStyle, hintWidth);

            GUILayout.Label("Copy Selected Node(s) to Buffer : Ctrl + C", hintsStyle, hintWidth);
            GUILayout.Label("Paste Node(s) from Buffer : Ctrl + V", hintsStyle, hintWidth);
            GUILayout.Label("Duplicate Selected Nodes : Ctrl + D", hintsStyle, hintWidth);

            GUILayout.Label("Snapping : Shift + Left Click + Mouse Drag", hintsStyle, hintWidth);
            GUILayout.Label("Multi-Selection Snapping : Ctrl + Shift + Left Click + Mouse Drag", hintsStyle, hintWidth);

            GUILayout.Label("Zoom : Mouse Wheel", hintsStyle, hintWidth);
            GUILayout.Label("Smooth Zoom : Ctrl + Middle Mouse + Mouse Drag", hintsStyle, hintWidth);

            GUILayout.Label("Press ''+'' on a node to add a new ''Field'' or ''Method'' ", hintsStyle, hintWidth);
            GUILayout.Label("Press ''x'' next to a ''Field'' or ''Method'' to remove it", hintsStyle, hintWidth);

            GUILayout.Label("Undo: Ctrl+Z", hintsStyle, hintWidth);
            GUILayout.Label("Redo: Ctrl+Y", hintsStyle, hintWidth);
        }
        #endregion

        #region Node Options
        showNodeOptions = EditorGUILayout.Foldout(showNodeOptions, "Node Options");
        if (showNodeOptions)
        {
            settings.connectionPointRadius = EditorGUILayout.Slider("Connection Point Radius", settings.connectionPointRadius, 1f, 10f);
            settings.associationColor = EditorGUILayout.ColorField("Association Color: ", settings.associationColor);
            settings.inheritanceColor = EditorGUILayout.ColorField("Inheritance Color: ", settings.inheritanceColor);
            settings.connectionHoverColor = EditorGUILayout.ColorField("Connection Hover Color", settings.connectionHoverColor);

        }
        #endregion

        #region Screenshot Options
        showScreenshotOptions = EditorGUILayout.Foldout(showScreenshotOptions, "Screenshot Options");
        if (showScreenshotOptions)
        {
            curContent = new GUIContent("Screenshot Sensitivity", "Time between each shot (higher is more reliable, lower is faster). \n If you have blank squares in your screenshot, incrase this value");
            settings.screenshotSensitivity = EditorGUILayout.Slider(curContent, settings.screenshotSensitivity, 0.2f, 1f);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Path:", EditorStyles.boldLabel, GUILayout.Width(70f));
            GUIStyle pathStyle = new GUIStyle(EditorStyles.label);
            pathStyle.wordWrap = true;
            EditorGUILayout.LabelField(settings.screenshotSavePath, pathStyle, GUILayout.Width(240), GUILayout.Height(50f));
            EditorGUILayout.EndHorizontal();

            curContent = new GUIContent("Choose Path", "Screenshot save path. Default is Assets folder.");
            bool selectPath = GUILayout.Button(curContent, buttonStyle);

            if (selectPath)
            {
                string savePath = EditorUtility.OpenFolderPanel("Choose Save Path", "Assets", "");
                if (savePath != null)
                    if (savePath != "")
                    {
                        string[] folders = savePath.Split('/');

                        bool saveNow = false;
                        string @final = "";
                        for (int i = 0; i < folders.Length; i++)
                        {
                            if (folders[i] == "Assets")
                                saveNow = true;
                            if (saveNow)
                                @final += folders[i] + "/";
                        }

                        @final = @final.TrimEnd('/');
                        settings.screenshotSavePath = @final;

                    }

            }

            EditorGUILayout.EndHorizontal();

            curContent = new GUIContent("Filename", "Name of the screenshot file. \n if file exist, new screenshot will be overwritten");
            settings.screenshotName = EditorGUILayout.TextField(curContent, settings.screenshotName);

            curContent = new GUIContent("Override File", "Override screenshot file if a file with same name exists");
            settings.overrideFile = EditorGUILayout.Toggle(curContent, settings.overrideFile);

            curContent = new GUIContent("Crop Blank Parts", "Save only the are where nodes are visible");
            settings.saveOnlyNodes = EditorGUILayout.Toggle(curContent, settings.saveOnlyNodes);

           
        }
        #endregion
        #region Debug Window
        debugWindow = EditorGUILayout.BeginToggleGroup("Debug (Developer Mode)", debugWindow);
        if (debugWindow)
        {
            debugShowUndoRedo = EditorGUILayout.BeginToggleGroup("Show Undo Redo", debugShowUndoRedo);
            if (debugShowUndoRedo)
            {
                GUILayout.Label("History Size :" + history.Count);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                undoScrollPos = GUILayout.BeginScrollView(undoScrollPos, GUILayout.MaxHeight(250f));
                GUILayout.Label("History Index :" + historyIndex);
                for (int i = 0; i < history.Count; i++)
                {
                    if (i == historyIndex)
                    {
                        GUILayout.Label("History Record: " + i, EditorStyles.boldLabel);
                    }
                    else
                    {
                        GUILayout.Label("History Record: " + i);
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndToggleGroup();

            GUILayout.Label("Mouse Position :" + mousePos.ToString());
            GUILayout.Label("Zoom Value : " + settings.zoomValue.ToString());
            GUILayout.Label("Graph Panel Size : " + graphPanel.size.ToString());

            if (targetConnection != null)
            {
                GUILayout.Label("Selected Connection : " + targetConnection.position.position);
            }

            if (selectedPoint != null)
            {
                GUILayout.Label("A IO_Point is currently selected!");
            }
            else
            {
                GUILayout.Label("No IO_Point selected!");
            }

            showSelectedNodes = EditorGUILayout.Foldout(showSelectedNodes, "Show Selected Nodes");
            if (showSelectedNodes)
            {
                GUILayout.Label(string.Format("Selected Nodes Count : {0}", selectedNodes.Count));
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label("Name: " + selectedNodes[i].name);
                    if (selectedNodes[i].parentGroup == null)
                        GUILayout.Label("Card: None");
                    else
                        GUILayout.Label("Card: " + selectedNodes[i].parentGroup.header);
                    GUILayout.Label("Position : " + selectedNodes[i].position.position.ToString());
                    GUILayout.Label("Visible Position :" + selectedNodes[i].position.position.ToString());
                    GUILayout.EndVertical();
                }
            }

            showActiveGroups = EditorGUILayout.Foldout(showActiveGroups, "Show Active Groups");
            if (showActiveGroups)
            {
                GUILayout.Label(activeGroups.Count + " Active Groups", EditorStyles.boldLabel);
                if (activeGroups.Count > 0)
                {
                    debug_groupPanelScrollPos = GUILayout.BeginScrollView(debug_groupPanelScrollPos, GUILayout.Height(250f));
                    for (int i = 0; i < activeGroups.Count; i++)
                    {
                        GUILayout.BeginVertical(EditorStyles.helpBox);
                        GUILayout.Label("Name: " + activeGroups[i].header);
                        GUILayout.Label("Position: " + activeGroups[i].position.position);
                        for (int j = 0; j < activeGroups[i].childNodes.Count; j++)
                        {
                            GUILayout.Label("Child (" + j + ") :" + activeGroups[i].childNodes[j].name);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndScrollView();
                }
            }

            GUILayout.Label("Window Size :" + position.size.ToString());
            GUILayout.Label("Options Panel Position: " + optionsPanel.position.ToString());
            GUILayout.Label("Options Panel Scale : " + optionsPanel.size.ToString());
            GUILayout.Space(10f);
            GUILayout.Label("Graph Panel Position: " + graphPanel.position.ToString());
            GUILayout.Label("Graph Panel Scale : " + graphPanel.size.ToString());
            GUILayout.Space(10f);
            GUILayout.Label("Offset : " + settings.offset.ToString());
            GUILayout.Space(10f);
            GUILayout.Label("Last center :" + lastCenter.ToString());
        }

        EditorGUILayout.EndToggleGroup();
        #endregion

        GUILayout.Space(10);
        settings.showGrid = EditorGUILayout.Toggle("Show Grid", settings.showGrid, toggle);//Bold Toggle
        settings.useStraightConnections = EditorGUILayout.Toggle("Straight Lines", settings.useStraightConnections, toggle);

        GUILayout.BeginHorizontal();
        GUIStyle g = new GUIStyle()
        {
            font =globalFont,
        };
        GUILayout.Space(6);
        GUILayout.Label("Class Texture",g,GUILayout.Width(146));
        settings.nodeClassTexture = (Texture2D)EditorGUILayout.ObjectField(settings.nodeClassTexture,typeof(Texture2D),false);
        GUILayout.EndHorizontal();
        bool takeScreenshot = GUILayout.Button("Take Screenshot of Current Screen",buttonStyle);

        if (takeScreenshot)
            TakeScreenshot();
        GUILayout.Space(10);
        settings.makeSerialiazable = EditorGUILayout.Toggle("Create serializable classes", settings.makeSerialiazable, toggle);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(settings.scriptSavePath, GUILayout.Width(activeOptionsPanelOffset / 2));
        bool _chooseClassPath = GUILayout.Button("Choose Script Generation Path", buttonStyle);


        if (_chooseClassPath)
        {
            string savePath = EditorUtility.OpenFolderPanel("Choose Save Path", settings.scriptSavePath, "");
            if (savePath != null)
                if (savePath != "")
                {
                    string[] folders = savePath.Split('/');

                    bool saveNow = false;
                    string @final = "";
                    for (int i = 0; i < folders.Length; i++)
                    {
                        if (folders[i] == "Assets")
                            saveNow = true;
                        if (saveNow)
                            @final += folders[i] + "/";
                    }

                    @final = @final.TrimEnd('/');
                    settings.scriptSavePath = @final;

                }

        }

        GUILayout.EndHorizontal();
        if ( GUILayout.Button("Generate Classes", buttonStyle))
            ContextCreateScripts();





        GUILayout.EndArea();
        GUILayout.EndArea();


        GUILayout.EndScrollView();


    OptionsPanelEnd:
        {

        }

        if (!screenshotMode)
        {
            if (settings.optionsPanelEnabled)
            {
                activeOptionsPanelOffset = Mathf.Lerp(activeOptionsPanelOffset, settings.optionsPanelOffset, 0.05f);
                settings.optionsPanelEnabled = GUI.Toggle(new Rect(position.width - activeOptionsPanelOffset - 25f, 2f, 22f, 22f), settings.optionsPanelEnabled, "-", EditorStyles.toolbarButton);
            }
            else
            {
                activeOptionsPanelOffset = Mathf.Lerp(activeOptionsPanelOffset,0,0.05f);
                settings.optionsPanelEnabled = GUI.Toggle(new Rect(position.width - activeOptionsPanelOffset - 25f, 2f, 22f, 22f), settings.optionsPanelEnabled, "+", EditorStyles.toolbarButton);
            }
        }

        //---------------------
        #endregion
        Repaint();
        AssetDatabase.SaveAssets();
        if (!screenshotMode)//Lock controls when taking screen shot
        {
            HandleEvents(current, mousePos);//Handle input etc.
        }
    }

    void OnDestroy()
    {
        NodeEditor window = (NodeEditor)GetWindow(typeof(NodeEditor));
        settings.windowPosition = window.position.position;
        settings.windowSize = window.position.size;
        saveSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    #endregion

    private int CardCountInNodes(List<Node> lst)
    {
        List<Group> differentCards = new List<Group>();

        foreach (var node in lst)
        {
            if (!differentCards.Contains(node.parentGroup) && node.parentGroup != null)
                differentCards.Add(node.parentGroup);
        }

        return differentCards.Count;
    }

    private List<Group> GetCardsFromNodes(List<Node> lst)
    {
        List<Group> cards = new List<Group>();
        foreach (var node in lst)
            if (node.parentGroup != null)
                cards.Add(node.parentGroup);
        return cards;
    }

}