using System;
using System.Buffers.Text;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Transformation {
    RectTransform rectTransform;
    Vector3 initialScale;
    Vector2 initialSizeDelta;
    public Transformation(RectTransform rectTransform) {
        this.rectTransform = rectTransform;
        initialScale = rectTransform.localScale;
        initialSizeDelta = rectTransform.sizeDelta;
    }

    public void ApplyScaleMod(float modifier) {
        rectTransform.localScale = new Vector3(initialScale.x * modifier, initialScale.y * modifier, initialScale.z * modifier);
    }
    
    public void ScaleWidth(float modifier) {
        rectTransform.sizeDelta = new Vector2(initialSizeDelta.x * modifier, initialSizeDelta.y);
    }
    
    public void ScaleHeight(float modifier) {
        rectTransform.sizeDelta = new Vector2(initialSizeDelta.x, initialSizeDelta.y * modifier);
    }

}