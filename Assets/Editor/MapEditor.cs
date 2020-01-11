using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI() {
        //상시 인스펙터의 변경사항을 게임에 반영
        //base.OnInspectorGUI();
        MapGenerator map = target as MapGenerator;
        if (DrawDefaultInspector()) {
            map.GenerateMap();
        }

        if (GUILayout.Button("Generate Map")) {
            map.GenerateMap();
        }
    }
}
