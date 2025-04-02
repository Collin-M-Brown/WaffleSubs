using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasFinder : MonoBehaviour
{
    public static CanvasFinder Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }
    
    public Transform GetUICanvasTransform() {
        return gameObject.transform;

    }

}
