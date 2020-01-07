using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Selectable
{
    public int ID;

    public bool selected;
    public bool hover;

    public Color baseColor;
    public Color outlineColor;

    public Rect position;
}
