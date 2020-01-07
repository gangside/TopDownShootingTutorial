using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#pragma warning disable 0168

[System.Serializable]
public class Node : Selectable
{
    public string name;

    [System.NonSerialized]
    public Node parentNode;
    public int parentNodeID = -1;//Default value is -1 since int is non-nullable. This is for serializing, -1 means there is no parent.

    public bool editNode = true;
    public bool editText;

    public IO_Point output;

    public IO_Point input;

    public Group parentGroup = null;

    public List<NodeField> nodeFields = new List<NodeField>();

    public List<NodeMethod> nodeMethods = new List<NodeMethod>();

    [System.NonSerialized]
    public List<Node> associatedNodes = new List<Node>();
    public List<int> associatedNodeIDs = new List<int>();

    [System.NonSerialized]
    public List<Node> childNodes = new List<Node>();

    [System.NonSerialized]
    public List<Connection> connections = new List<Connection>();

    [System.NonSerialized]
    private float targetHeight;

    public Node rootNode
    {
        get
        {
            Node curParent = this;
            while(curParent.parentNode != null)
            {
                curParent = curParent.parentNode;
            }
            return curParent;
        }
    }

    public int nodeColorID;

    public NodeColor nodeColor
    {
        get
        {
            return settings.presetColors[nodeColorID];
        }
    }

    public static SettingsData settings;

    private static NodeElement selectedNodeElement;

    private static GUIStyle buttonStyle;
    private static GUIStyle gui_style;
    private static GUIStyle Xstyle;

    private static GUIStyle[] customStyles;
    public static void Init(GUIStyle[] styleContainer)
    {
        customStyles = styleContainer;

        buttonStyle = GetStyleByName("pd_button");
        buttonStyle.margin = new RectOffset(0, 0, 2, 0);
        buttonStyle.stretchHeight = false;
        buttonStyle.stretchWidth = false;

        gui_style = GetStyleByName("pd_name_field_blue");//377
        gui_style.hover = GetStyleByName("pd_name_field_green").normal;//378
        gui_style.focused = GetStyleByName("pd_name_field_green").normal;
        gui_style.onFocused = GetStyleByName("pd_name_field_green").normal;
        gui_style.onActive = GetStyleByName("pd_name_field_green").normal;
        gui_style.onNormal = GetStyleByName("pd_name_field_green").normal;

        Xstyle = GetStyleByName("pd_toolbar");//515
    }

    public static GUIStyle GetStyleByName(string name)
    {
        return customStyles.Where(x => x.name == name).Select(x => x).ElementAt(0);
    }

    public void CreateIOPositions(float padding)
    {
        input.position.position = position.position + new Vector2(-14- padding, position.size.y / 2);
        output.position.position = position.position + new Vector2(position.size.x - 14+ padding+4, position.size.y / 2);

        input.position.size = new Vector2(24,24);
        output.position.size = input.position.size;
    }

    #region Constructors
    public Node(Rect _position, string _name)
    {
        position = _position;
        name = _name;

        input = new IO_Point();
        output = new IO_Point();

        input.parentNode = this;
        output.parentNode = this;
        nodeColorID = 0;

        AddField("Name", ProtectionLevel.@public, FieldType.@string);
     
       AddMethod("GetName", FieldType.@string,ProtectionLevel.@public);
    }

    public Node(Node node)
    {
        position = node.position;
        baseColor = node.baseColor;
        nodeFields = new List<NodeField>();
        nodeColorID = node.nodeColorID;

        for (int i = 0; i < node.nodeFields.Count; i++)
        {
            NodeField newField = new NodeField();
            newField.fieldType = node.nodeFields[i].fieldType;
            newField.protectionLevel = node.nodeFields[i].protectionLevel;
            newField.name = node.nodeFields[i].name;
            nodeFields.Add(newField);
        }

        for (int i = 0; i < node.nodeMethods.Count; i++)
        {
            NodeMethod newMethod = new NodeMethod();
            newMethod.protectionLevel = node.nodeMethods[i].protectionLevel;
            newMethod.fieldType = node.nodeMethods[i].fieldType;
            newMethod.name = node.nodeMethods[i].name;
            nodeMethods.Add(newMethod);
        }

        name = node.name + "(Copy)";

        input = new IO_Point();
        output = new IO_Point();

        input.parentNode = this;
        output.parentNode = this;
    }

    public Node(Node node, bool changeName)
    {
        position = node.position;
        baseColor = node.baseColor;
        nodeColorID = node.nodeColorID;
        nodeFields = new List<NodeField>();

        for (int i = 0; i < node.nodeFields.Count; i++)
        {
            NodeField newField = new NodeField();
            newField.fieldType = node.nodeFields[i].fieldType;
            newField.protectionLevel = node.nodeFields[i].protectionLevel;
            newField.name = node.nodeFields[i].name;
            nodeFields.Add(newField);
        }

        for (int i = 0; i < node.nodeMethods.Count; i++)
        {
            NodeMethod newMethod = new NodeMethod();
            newMethod.protectionLevel = node.nodeMethods[i].protectionLevel;
            newMethod.fieldType = node.nodeMethods[i].fieldType;
            newMethod.name = node.nodeMethods[i].name;
            nodeMethods.Add(newMethod);
        }

        if (changeName)
        {
            name = node.name + "(Copy)";
        }
        else
        {
            name = node.name;
        }

        editNode = node.editNode;
        parentNodeID = node.parentNodeID;

        input = new IO_Point();
        output = new IO_Point();

        input.parentNode = this;
        output.parentNode = this;
    }
    #endregion

    public void DrawConnectionPoints(float padding, Color col)
    {
        CreateIOPositions(padding);

        GUI.color = new Color(1F, .1F, .3f, 0);

        EditorGUI.DrawTextureTransparent(input.position, settings.nodeInputConnectionTexture);
        EditorGUI.DrawTextureTransparent(output.position, settings.nodeOutputConnectionTexture);
        GUI.color = Color.white;
    }

    public bool Draw_Node(Rect graphPanel,string[] customClasses, bool isTarget, TexturePack texturePack, out bool deleteNode, Font font,GUISkin skin)
    {
        bool recordState = false;
        deleteNode = false;
        float lineHeight = 48;

        position.width = settings.nodeWidth;

        position.x = Mathf.Clamp(position.x, graphPanel.xMin, graphPanel.xMax - position.width);
        position.y = Mathf.Clamp(position.y, graphPanel.yMin, graphPanel.yMax - position.height);

        nodeColorID = rootNode.nodeColorID;

        foreach (Connection con in input.connections)
        {
            switch (con.type)
            {
                case ConnectionType.Association:
                    {
                        DrawingUtilities.DrawTriangle(new Vector2(input.position.center.x - settings.connectionStartThreshold * 3 / 4, input.position.center.y), 8, settings.associationColor, DrawingUtilities.TrianglePoint.Right);
                    }
                    break;
                case ConnectionType.Inheritance:
                    {
                        DrawingUtilities.DrawTriangle(new Vector2(input.position.center.x - settings.connectionStartThreshold * 3 / 4, input.position.center.y), 8, settings.inheritanceColor, DrawingUtilities.TrianglePoint.Right);
                    }
                    break;
            }
        }

        foreach (Connection con in output.connections)
        {
            switch (con.type)
            {
                case ConnectionType.Association:
                    {
                        DrawingUtilities.DrawTriangle(new Vector2(output.position.center.x + settings.connectionStartThreshold * 3 / 4, output.position.center.y), 8, settings.associationColor, DrawingUtilities.TrianglePoint.Right);
                    }
                    break;
                case ConnectionType.Inheritance:
                    {
                        DrawingUtilities.DrawTriangle(new Vector2(output.position.center.x + settings.connectionStartThreshold * 3 / 4, output.position.center.y), 8, settings.inheritanceColor, DrawingUtilities.TrianglePoint.Right);
                    }
                    break;
            }
        }

        float l = 20f;
        Rect classRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect attributesRect = new Rect(position.x , position.y + lineHeight-6,
            position.width ,
            nodeFields.Count*l+l+12);
        Rect footerRect = new Rect(position.x, attributesRect.y+attributesRect.height,
            position.width,
            (nodeMethods.Count+1)* l );

        targetHeight = nodeMethods.Count * l + nodeFields.Count * l + l + lineHeight + 40;
        position.height = Mathf.Lerp(position.height, targetHeight, 0.1f);

        #region ClassRect

        GUI.Box(position, "", selected ? 
            customStyles[nodeColor.onId] :
            customStyles[nodeColor.offId]);

        GUI.color = Color.white;

        Rect classImageRect = new Rect(position.x+8, position.y+8, lineHeight-16, lineHeight-16);
        if(GUI.Button(classImageRect,"", EditorStyles.label))
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < settings.presetColors.Length; i++)
            {
                menu.AddItem(new GUIContent(settings.presetColors[i].name), false, SetColor, i);
            }

            EditorZoomArea.End();
            menu.ShowAsContext();
            Event.current.Use();
            EditorZoomArea.Begin(settings.zoomValue, graphPanel, new Rect(0, 0, position.width, position.height), settings.offset, EditorZoomArea.mousePos);
            recordState = true;
        } 
        GUI.Label(classImageRect, settings.nodeClassTexture);

        Handles.color = new Color(.4f,.4f,.4f,.8f);
        Handles.DrawLine(new Vector3(position.x+lineHeight+2,position.y+4),new Vector3(position.x + lineHeight + 2, position.y+lineHeight-8));
        Handles.DrawLine(new Vector3(position.x + 2, position.y + lineHeight -6), new Vector3(position.width+position.x-2, position.y + lineHeight -6));

        Rect classTextRect = new Rect(position.x+lineHeight+8, position.y+20, position.width-lineHeight*2, lineHeight-30);

        #region Title
        string title = name + (parentNode != null ? (" : " + parentNode.name) : "");

        if (editNode)
            name = GUI.TextField(classTextRect, name, gui_style);
        else
            GUI.Label(classTextRect, title, gui_style);

        if(Event.current.keyCode == KeyCode.Return ||
            (Event.current.type == EventType.MouseDown && !classTextRect.Contains(Event.current.mousePosition))||
            Event.current.keyCode == KeyCode.KeypadEnter)
        {
            GUI.FocusControl(null);
        }
        #endregion

        #endregion

        deleteNode = GUI.Button(new Rect(classRect.x + classRect.width - 18, classRect.y + 2, 16, 16), settings.deleteTexture, Xstyle);

        if (editNode)
            editNode = GUI.Toggle(new Rect(classRect.x + classRect.width - 36, classRect.y + 2, 16, 16), editNode,settings.settingsTexture, Xstyle);
        else
            editNode = GUI.Toggle(new Rect(classRect.x + classRect.width - 36, classRect.y + 2, 16, 16), editNode, "", Xstyle);

        if (editNode)
        {
            targetHeight = 20f * (nodeFields.Count + nodeMethods.Count) + 75f;
        }
        else
        {
            targetHeight = 18f * (nodeFields.Count + nodeMethods.Count) + 75f;
        }

        GUI.backgroundColor = Color.white;

        #region AttributesRect
        /* GUI.color = settings.nodeAttributesRectBackgroundColor;
         GUI.BeginGroup(attributesRect, GUI.skin.box);
         GUI.color = Color.white;
         GUI.EndGroup();*/
        bool fieldChanged = Draw_Attributes(attributesRect, font,skin,customClasses) ;
        Handles.color = new Color(.4f, .4f, .4f, .8f);

        Handles.DrawLine(new Vector3(position.x  + 2, footerRect.y-6), new Vector3(position.x + position.width-4, footerRect.y - 6));
        Handles.DrawLine(new Vector3(position.x + 2, footerRect.y-2 ), new Vector3(position.x + position.width - 4, footerRect.y-2));
        #endregion

        bool functionChanged = Draw_Functions(footerRect, skin,customClasses);
        #region Connections
        DrawConnectionPoints(10, Color.yellow);
        #endregion

        return fieldChanged || functionChanged || recordState;
    }

    public bool Draw_Attributes(Rect container, Font font, GUISkin skin,string[] customClasses)
    {
        bool rec = false;

        GUIStyle txtStyle = new GUIStyle(EditorStyles.textField)
        {
            stretchHeight = false,
            stretchWidth = false
        };

        GUILayout.BeginArea(container);


        for (int i = 0; i < nodeFields.Count; i++)
        {
            const int FieldTypeWidth = 78;
            const int ProtectionWidth = 60;

            NodeField field = nodeFields[i];
            if (!editNode)
            {
                string protectionMark = "+";
                if (field.protectionLevel == ProtectionLevel.@private)
                    protectionMark = "-";
                else if(field.protectionLevel == ProtectionLevel.@protected)
                    protectionMark = "o";

                GUIContent content = new GUIContent(protectionMark+" "+field.name+" : " +field.fieldType);
                GUILayout.Label(content);
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(container.width - 2f));
                field.name = GUILayout.TextField(field.name, txtStyle,GUILayout.Width(FieldTypeWidth));

                bool showProtectionLevelMenu = GUILayout.Button(new GUIContent(field.protectionLevel.ToString()), buttonStyle, GUILayout.MaxWidth(ProtectionWidth));
                Rect last = GUILayoutUtility.GetLastRect();
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(last.x+last.width-8,last.y+6,6,6),settings.arrowTexture);

                bool showFieldTypeMenu;
                if(field.isCustom)
                    showFieldTypeMenu = GUILayout.Button(new GUIContent(field.customName), buttonStyle, GUILayout.MaxWidth(FieldTypeWidth));
                else
                    showFieldTypeMenu = GUILayout.Button(new GUIContent(field.fieldType.ToString()), buttonStyle, GUILayout.MaxWidth(FieldTypeWidth));

                last = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(new Rect(last.x + last.width - 10, last.y + 6, 6, 6), settings.arrowTexture);

                GUILayout.Space(3);
                if (GUILayout.Button(new GUIContent(settings.deleteTexture), Xstyle, GUILayout.MaxWidth(20), GUILayout.Width(20)))
                    nodeFields.RemoveAt(i);


                GUILayout.EndHorizontal();

                #region Buttons
                if (showProtectionLevelMenu)
                {
                    Vector2 mousePos = EditorZoomArea.mousePos;
                    Rect graphPanel = EditorZoomArea.graphPanel;

                    GenericMenu menu = new GenericMenu();

                    selectedNodeElement = field;

                    menu.AddItem(new GUIContent("private"), false, SetProtectionLevel, ProtectionLevel.@private);
                    menu.AddItem(new GUIContent("public"), false, SetProtectionLevel, ProtectionLevel.@public);
                    menu.AddItem(new GUIContent("protected"), false, SetProtectionLevel, ProtectionLevel.@protected);

                    EditorZoomArea.End();

                    menu.ShowAsContext();
                    Event.current.Use();

                    EditorZoomArea.Begin(settings.zoomValue, graphPanel, new Rect(0, 0, position.width, position.height), settings.offset, mousePos);

                    rec = true;
                }
                if (showFieldTypeMenu)
                {
                    Vector2 mousePos = EditorZoomArea.mousePos;
                    Rect graphPanel = EditorZoomArea.graphPanel;

                    GenericMenu menu = new GenericMenu();

                    selectedNodeElement = field;


                    //primitives
                    for (int j = 0; j < (int)FieldType.numberOfFields; j++)
                        if(!((FieldType)j).ToString().TrimStart('@').Equals("void"))
                            menu.AddItem(new GUIContent("Primitives/" + ((FieldType)j).ToString().TrimStart('@')), false, SetFieldType, (FieldType)j);

                    //arrays
                    for (int j = 0; j < (int)FieldType.numberOfFields; j++)
                        if(!((FieldType)j).ToString().TrimStart('@').Equals("void"))
                            menu.AddItem(new GUIContent("Arrays/" + ((FieldType)j).ToString().TrimStart('@')+"[]"), false, SetFieldType, ((FieldType)j).ToString().TrimStart('@') + "[]");

                    //lists
                    for (int j = 0; j < (int)FieldType.numberOfFields; j++)
                        if(!((FieldType)j).ToString().TrimStart('@').Equals("void"))
                            menu.AddItem(new GUIContent("Lists/" + "List<"+((FieldType)j).ToString().TrimStart('@')+">"), false, SetFieldType, "List<"+((FieldType)j).ToString().TrimStart('@')+">");


                    //custom classes
#if UNITY_2018_2_OR_NEWER
                    menu.AddDisabledItem(new GUIContent(settings.birchgamesTexture),false);
#else
                    menu.AddDisabledItem(new GUIContent(settings.birchgamesTexture));
#endif

                    foreach (var t in customClasses)
                    {
                        menu.AddItem(new GUIContent("Custom Classes/" + t), false, SetFieldType, t);
                        menu.AddItem(new GUIContent("Arrays/Custom Classes/" + t), false, SetFieldType, t+"[]");
                         menu.AddItem(new GUIContent("Lists/Custom Classes/" + "List<"+t+">"), false, SetFieldType, "List<"+t+">");
                    }

                    EditorZoomArea.End();

                    menu.ShowAsContext();
                    Event.current.Use();

                    EditorZoomArea.Begin(settings.zoomValue, graphPanel, new Rect(0, 0, position.width, position.height), settings.offset, mousePos);

                    rec = true;
                }
#endregion
            }
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Space(15);
        bool addField = GUILayout.Button("Add New Field", EditorStyles.toolbarButton,GUILayout.Width(container.width-30));
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        if (addField && editNode)
        {
            AddField("newField",ProtectionLevel.@public,FieldType.@GameObject);
            rec = true;
        }

        GUILayout.EndArea();

        return rec;
    }

    public bool Draw_Functions(Rect container,GUISkin skin,string[] customClasses)
    {
        bool rec = false;

        GUIStyle txtStyle = new GUIStyle(EditorStyles.textField)
        {
            stretchHeight = false,
            stretchWidth = false
        };

        GUILayout.BeginArea(container);


        for (int i = 0; i < nodeMethods.Count; i++)
        {
            const int FieldTypeWidth = 78;
            const int ProtectionWidth = 60;

            NodeMethod method = nodeMethods[i];
            if (!editNode)
            {
                string protectionMark = "+";
                if (method.protectionLevel == ProtectionLevel.@private)
                    protectionMark = "-";
                else if (method.protectionLevel == ProtectionLevel.@protected)
                    protectionMark = "o";

                GUIContent content = new GUIContent(protectionMark + " " + method.name + " : " + method.fieldType);
                GUILayout.Label(content);
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.MaxWidth(container.width - 2f));
                method.name = GUILayout.TextField(method.name, txtStyle, GUILayout.Width(FieldTypeWidth));

                bool showProtectionLevelMenu = GUILayout.Button(new GUIContent(method.protectionLevel.ToString()), buttonStyle, GUILayout.MaxWidth(ProtectionWidth));
                Rect last = GUILayoutUtility.GetLastRect();
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(last.x + last.width - 8, last.y + 6, 6, 6), settings.arrowTexture);

                bool showFieldTypeMenu;
                if(method.isCustom)
                    showFieldTypeMenu = GUILayout.Button(new GUIContent(method.customName), buttonStyle, GUILayout.MaxWidth(FieldTypeWidth));
                else
                    showFieldTypeMenu = GUILayout.Button(new GUIContent(method.fieldType.ToString()), buttonStyle, GUILayout.MaxWidth(FieldTypeWidth));
                last = GUILayoutUtility.GetLastRect();
                GUI.DrawTexture(new Rect(last.x + last.width - 10, last.y + 6, 6, 6), settings.arrowTexture);

                GUILayout.Space(3);
                if (GUILayout.Button(new GUIContent(settings.deleteTexture), Xstyle, GUILayout.MaxWidth(20), GUILayout.Width(20)))
                    nodeMethods.RemoveAt(i);


                GUILayout.EndHorizontal();



#region Buttons
                if (showProtectionLevelMenu)
                {
                    Vector2 mousePos = EditorZoomArea.mousePos;
                    Rect graphPanel = EditorZoomArea.graphPanel;

                    GenericMenu menu = new GenericMenu();

                    selectedNodeElement = method;

                    menu.AddItem(new GUIContent("private"), false, SetProtectionLevel, ProtectionLevel.@private);
                    menu.AddItem(new GUIContent("public"), false, SetProtectionLevel, ProtectionLevel.@public);
                    menu.AddItem(new GUIContent("protected"), false, SetProtectionLevel, ProtectionLevel.@protected);

                    EditorZoomArea.End();

                    menu.ShowAsContext();
                    Event.current.Use();

                    EditorZoomArea.Begin(settings.zoomValue, graphPanel, new Rect(0, 0, position.width, position.height), settings.offset, mousePos);
                    rec = true;
                }
                if (showFieldTypeMenu)
                {
                    Vector2 mousePos = EditorZoomArea.mousePos;
                    Rect graphPanel = EditorZoomArea.graphPanel;

                    GenericMenu menu = new GenericMenu();

                    selectedNodeElement = method;

                 //primitives
                    for (int j = 0; j < (int)FieldType.numberOfFields; j++)
                            menu.AddItem(new GUIContent("Primitives/" + ((FieldType)j).ToString().TrimStart('@')), false, SetFieldType, (FieldType)j);

                    //arrays
                    for (int j = 0; j < (int)FieldType.numberOfFields; j++)
                        if(!((FieldType)j).ToString().TrimStart('@').Equals("void"))
                            menu.AddItem(new GUIContent("Arrays/" + ((FieldType)j).ToString().TrimStart('@')+"[]"), false, SetFieldType, ((FieldType)j).ToString().TrimStart('@') + "[]");

                    //lists
                    for (int j = 0; j < (int)FieldType.numberOfFields; j++)
                        if(!((FieldType)j).ToString().TrimStart('@').Equals("void"))
                            menu.AddItem(new GUIContent("Lists/" + "List<"+((FieldType)j).ToString().TrimStart('@')+">"), false, SetFieldType, "List<"+((FieldType)j).ToString().TrimStart('@')+">");


                    //custom classes
#if UNITY_2018_2_OR_NEWER
                    menu.AddDisabledItem(new GUIContent(settings.birchgamesTexture),false);
#else
                    menu.AddDisabledItem(new GUIContent(settings.birchgamesTexture));
#endif
                    foreach (var t in customClasses)
                    {
                        menu.AddItem(new GUIContent("Custom Classes/" + t), false, SetFieldType, t);
                        menu.AddItem(new GUIContent("Arrays/Custom Classes/" + t), false, SetFieldType, t+"[]");
                         menu.AddItem(new GUIContent("Lists/Custom Classes/" + "List<"+t+">"), false, SetFieldType, "List<"+t+">");
                    }

                    EditorZoomArea.End();

                    menu.ShowAsContext();
                    Event.current.Use();

                    EditorZoomArea.Begin(settings.zoomValue, graphPanel, new Rect(0, 0, position.width, position.height), settings.offset, mousePos);
                    rec = true;
                }
#endregion
            }
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Space(15);
        bool addMethod = GUILayout.Button("Add New Method", EditorStyles.toolbarButton, GUILayout.Width(container.width - 30));
        GUILayout.EndHorizontal();

        if (addMethod )
        {
            AddMethod("funcF",FieldType.@GameObject, ProtectionLevel.@public );
            rec = true;
        }

        GUILayout.Space(24);
        GUILayout.EndArea();
        return rec;
    }


    private void SetFieldType(object recData)
    {
        try
        {
            selectedNodeElement.fieldType = (FieldType)recData;
            selectedNodeElement.isCustom = false;
        }
        catch(InvalidCastException cast)
        {
            try
            {
                string data = (string) recData;
                selectedNodeElement.isCustom = true;
                selectedNodeElement.customName = data;

            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }


    private void SetColor(object recData)
    {
        nodeColorID = (int)recData;
    }

    private void SetProtectionLevel(object recData)
    {
        selectedNodeElement.protectionLevel = (ProtectionLevel)recData;
    }

    private void AddField(string name, ProtectionLevel protectionLevel, FieldType fieldType)
    {
        NodeField newElement = new NodeField()
        {
            protectionLevel = protectionLevel,
            fieldType = fieldType,
            name = name
        };
        nodeFields.Add(newElement);
    }

    private void AddMethod(string name, FieldType fieldType,ProtectionLevel protectionLevel)
    {
        NodeMethod nodeMethod = new NodeMethod()
        {
            name=name,
            fieldType = fieldType,
            protectionLevel= protectionLevel
        };
        nodeMethods.Add(nodeMethod);
    }

  
}