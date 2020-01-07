using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "StyleContainer", order = 1)]
public class StyleContainer : ScriptableObject
{
    public List<GUIStyle> customStyles = new List<GUIStyle>();
    public List<int> styleIndexes = new List<int>();
    public List<bool> showPreview = new List<bool>();
}
