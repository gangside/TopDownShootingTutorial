using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NodeElement
{
    public string name;
    public ProtectionLevel protectionLevel;
    public FieldType fieldType;
    public bool isCustom=false;
    public string customName;
}

[System.Serializable]
public class NodeField : NodeElement
{
 
}

[System.Serializable]
public class NodeMethod : NodeElement
{
}


[System.Serializable]
public enum ProtectionLevel
{
    @public,
    @protected,
    @private,
}

[System.Serializable]
public enum FieldType
{
    @string,
    @int,
    @float,
    @bool,
    @char,
    @Color,
    @GameObject,
    @Object,
    @Transform,
    @Quaternion,
    @Vector3,
    @Vector2,
    @void,
    numberOfFields,

}
