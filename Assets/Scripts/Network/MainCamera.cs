using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Linq;

public class MainCam : MonoBehaviour {
    public static MainCam Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }
    
    public Camera GetMainCam() {
        return gameObject.GetComponent<Camera>();
    }
}