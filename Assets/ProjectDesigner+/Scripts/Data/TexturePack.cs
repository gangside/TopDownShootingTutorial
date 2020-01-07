using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TexturePack {

    public Texture2D classTexture;
    private string classTexturePath = "Assets/UnityUMLPlus/Textures/Class.png";

    public Texture2D classRectTexture;
    private string classRectTexturePath = "Assets/UnityUMLPlus/Textures/Gradient.png";

    public Texture2D sphereInTexture;
    private string sphereInTexturePath = "Assets/UnityUMLPlus/Textures/SphereIn.png";

    public Texture2D sphereOutTexture;
    private string sphereOutTexturePath = "Assets/UnityUMLPlus/Textures/SphereOut.png";

    public Texture2D inheritanceArrow;
    private string inheritanceArrowPath = "Assets/UnityUMLPlus/Textures/InheritanceArrow.png";


    public void Load()
    {
        classTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(classTexturePath, typeof(Texture2D));
        classRectTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(classRectTexturePath, typeof(Texture2D));
        sphereInTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(sphereInTexturePath, typeof(Texture2D));
        sphereOutTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(sphereOutTexturePath, typeof(Texture2D));
        inheritanceArrow = (Texture2D)AssetDatabase.LoadAssetAtPath(inheritanceArrowPath, typeof(Texture2D));
    }
}
