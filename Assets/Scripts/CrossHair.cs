using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossHair : MonoBehaviour
{
    public LayerMask targetMask;
    public SpriteRenderer dot;
    public Color dotHighlightColor;
    Color originalColor;

    private void Start() {
        Cursor.visible = false;
        originalColor = dot.color;
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * 40 * Time.deltaTime);
    }

    public void DetectTarget(Ray ray) {
        if(Physics.Raycast(ray, 100, targetMask)) {
            dot.color = dotHighlightColor;
        }
        else {
            dot.color = originalColor;
        }
    }
}
