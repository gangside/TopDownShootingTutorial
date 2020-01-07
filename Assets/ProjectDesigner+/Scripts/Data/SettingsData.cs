using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SettingsData : ScriptableObject {
    public Vector2 windowPosition = new Vector2(300f, 300f);
    public Vector2 windowSize = new Vector2(1024f,1024f);
    public Vector2 offset = new Vector2(2500f, 2500f);

    public Color optionsPanelColor = Color.gray;
    public Color graphPanelColor = new Color(32f / 255f, 32f / 255f, 32f / 255f);
    public Color gridLineColor = new Color(64f / 255f, 64f / 255f, 64f / 255f, 255f);

    public bool showGrid = true;
    public bool optionsPanelEnabled = true;

    public float gridSpacing = 50f;
    public float xGraphPanelSize = 5000f;
    public float yGraphPanelSize = 5000f;

    public float optionsPanelOffset = 250f;

    public float zoomValue = 1f;

    public bool overrideFile = false;
    public bool saveOnlyNodes = false;


    public string contentSavePath = "Assets/ProjectDesigner+/Data/Content";
    public string contentName = "EditorContent";
    public Texture arrowTexture;



    [Space(4)]
    [Header("ScreenShot Properties")]
    public string screenshotSavePath = "Assets/ProjectDesigner+/Data/Content/Screenshots";
    public string screenshotName = "Screenshot";
    public float screenshotSensitivity = 0.5f;

    [Space(4)]
    [Header("Node Attributes")]
    public float nodeWidth = 250f;
    public Texture2D nodeClassTexture;
    public Texture2D deleteTexture;
    public Texture2D settingsTexture;


    [Space(4)]
    
    [Space(4)] [Header("Card Attributes")]
    public Color cardBgColor = new Color(.2f,.2f,.2f,.4f); 

    [Space(4)]

    [Header("Connection Attributes")]

    public bool useStraightConnections = false;
    public Color associationColor = new Color(150f / 255f, 68f / 255f, 42f / 255f);
    public Color inheritanceColor = new Color(1f, 1f, 1f, 1f);
    public Color connectionHoverColor = Color.blue;
    public float connectionPointRadius = 10f;
    public float connectionStartThreshold = 30f;
    public Texture2D nodeConnectionTexture;
    public Texture2D nodeInputConnectionTexture;
    public Texture2D nodeOutputConnectionTexture;

    [Space(4)] [Header("Script Generation")]

    public bool makeSerialiazable = true;

    public string scriptSavePath = "Assets/Scripts";

    [Space(4)]
    [Header("Custom Styles - Please do not change")]

    public NodeColor[] presetColors = new NodeColor[7].Select(x => new NodeColor("NewColor",0, 0)).ToArray();

    public Texture birchgamesTexture;

    public StyleContainer styleContainer;
}
