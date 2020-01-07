using UnityEngine;

public static class RectExtensions
{
    public static Vector2 TopLeft(this Rect rect)
    {
        return new Vector2(rect.xMin, rect.yMin);
    }
    public static Vector2 BottomLeft(this Rect rect)
    {
        return new Vector2(rect.x, rect.y + rect.height);
    }
    public static Vector2 BottomRight ( this Rect rect)
    {
        return new Vector2(rect.x + rect.width, rect.y + rect.height);
    }

    public static Rect Lerp(this Rect from, Rect to, float t)
    {
        from.x = Mathf.Lerp(from.x, to.x,t);
        from.y = Mathf.Lerp(from.y, to.y, t);
        from.width = Mathf.Lerp(from.width, to.width, t);
        from.height = Mathf.Lerp(from.height, to.height, t);
        return from;
    }
    public static Vector2 TopRight ( this Rect rect)
    {
        return new Vector2(rect.x + rect.width, rect.y);
    }
    public static Vector2 Right(this Rect rect)
    {
        return new Vector2(rect.xMax, rect.y + rect.height / 2);
    }
    public static Vector2 Left(this Rect rect)
    {
        return new Vector2(rect.x, rect.y + rect.height / 2);
    }
    public static Vector2 Middle(this Rect rect)
    {
        return rect.center;
    }
    public static Rect ScaleSizeBy(this Rect rect, float scale)
    {
        return rect.ScaleSizeBy(scale, rect.center);
    }
    public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;
        result.xMin *= scale;
        result.xMax *= scale;
        result.yMin *= scale;
        result.yMax *= scale;
        result.x += pivotPoint.x;
        result.y += pivotPoint.y;
        return result;
    }
    public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
    {
        return rect.ScaleSizeBy(scale, rect.center);
    }
    public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;
        result.xMin *= scale.x;
        result.xMax *= scale.x;
        result.yMin *= scale.y;
        result.yMax *= scale.y;
        result.x += pivotPoint.x;
        result.y += pivotPoint.y;
        return result;
    }
}

public class EditorZoomArea
{
    public static Vector2 mousePos;
    public static Rect graphPanel;
    public const float kEditorWindowTabHeight = 21.0f;
    public static bool screenshotMode;
    private static Matrix4x4 _prevGuiMatrix;
    private static Rect screen;
    private static bool drawGUI;

    public static Rect Begin(float zoomScale, Rect screenCoordsArea, Rect clipArea, Vector2 offset,Vector2 mousePosition)
    {
        screen = clipArea;
        graphPanel = screenCoordsArea;
        mousePos = mousePosition;

        GUI.EndGroup(); // End the card Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.

        //Fake editor window
        if (!screenshotMode)
        {
            GUI.DrawTexture(new Rect(0, 0, screen.width, screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.gray, 0, 0);
        }
        //--

        screenCoordsArea.y += kEditorWindowTabHeight;

        screenCoordsArea.position += offset;

        screenCoordsArea.size *= zoomScale + 0.001f;

        Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());

        GUI.BeginGroup(clippedArea);

        _prevGuiMatrix = GUI.matrix;
        Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
        Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
        GUI.matrix = translation * scale * translation.inverse;


        return clippedArea;
    }

    public static void End()
    {
        GUI.matrix = _prevGuiMatrix;

        GUI.EndGroup();

        #region Fake Editor Window
        if (!screenshotMode)
        {
            GUIStyle buttonStyle = new GUIStyle();
            GUIStyle tabStyle = new GUIStyle(UnityEditor.EditorStyles.toolbar);
            tabStyle.alignment = TextAnchor.MiddleLeft;
            tabStyle.fontSize = 11;
            tabStyle.fixedHeight = 15f;

            Texture2D backGround = Texture2D.whiteTexture;
            Color[] colors = backGround.GetPixels();
            Color backgroundColor = new Color(.3f, .3f, .3f, 1f);
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] *= backgroundColor;
            }
            backGround.SetPixels(colors);
            buttonStyle.fontSize = 10;
            buttonStyle.hover.background = backGround;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            GUI.DrawTexture(new Rect(0f, 0f, screen.width, 21f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.gray, 0, 0);//Top bar
            GUI.DrawTexture(new Rect(0f, screen.height + 21f, screen.width, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, Color.gray, 0, 0);//Bottom Line
            GUI.Button(new Rect(0f, 6f, 98f, 15f), "NodeEditor", tabStyle);//Tab //TODO: Show all tabs here if possible
            GUI.Button(new Rect(screen.width - 17.5f, 0f, 13f, 13f), "X", buttonStyle);//Exit button
            GUI.Button(new Rect(screen.width - 29.5f, 0f, 13f, 13f), "□", buttonStyle);//Maximize-Minimize
            buttonStyle.alignment = TextAnchor.UpperLeft;
            buttonStyle.fontSize = 7;
            GUI.Button(new Rect(screen.width - 20f, 14f, 16f, 8f), "▼☰", buttonStyle);//Window Menu
        }
        #endregion

        GUI.BeginGroup(new Rect(0.0f, kEditorWindowTabHeight, Screen.width, Screen.height));
    }
}